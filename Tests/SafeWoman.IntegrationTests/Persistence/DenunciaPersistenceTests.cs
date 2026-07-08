using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.IntegrationTests.Fixtures;

namespace SafeWoman.IntegrationTests.Persistence;

/// <summary>
/// Verifica que el flujo completo Denuncia + Denunciado + Evidencias se persiste
/// correctamente con sus relaciones. Este es el flujo REAL de la app: una
/// denuncia formal con foto de agresor y 2 evidencias multimedia.
/// </summary>
[Collection("Postgres")]
public class DenunciaPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;

    public DenunciaPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Persistir_una_denuncia_completa_debe_guardar_relaciones_correctamente()
    {
        // Arrange — víctima con una denuncia + denunciado + 2 evidencias
        using var db = await _fixture.CrearDbContextAsync();

        var victima = Victima.Crear("Ana Prueba", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        var denuncia = Denuncia.CrearFormal(
            idVictima: victima.IdVictima,
            fotoDniRuta: "/uploads/dni.jpg",
            departamento: "Ayacucho",
            provincia: "Huamanga",
            distrito: "Ayacucho",
            referenciaUbicacion: "Av. Ramón Castilla 234",
            lat: -13.1587m,
            lng: -74.2237m,
            fechaHecho: new DateOnly(2026, 7, 1),
            horaHecho: new TimeOnly(23, 30),
            descripcion: "Fue agredida por su pareja.");
        db.Denuncias.Add(denuncia);
        await db.SaveChangesAsync();

        var denunciado = Denunciado.Crear(denuncia.IdDenuncia, "Juan Pérez", RelacionVictima.Pareja);
        db.Denunciados.Add(denunciado);

        db.Evidencias.Add(Evidencia.Crear(denuncia.IdDenuncia, "foto1.jpg", "/uploads/foto1.jpg", TipoArchivo.Imagen, 500_000));
        db.Evidencias.Add(Evidencia.Crear(denuncia.IdDenuncia, "video1.mp4", "/uploads/video1.mp4", TipoArchivo.Video, 2_000_000));
        await db.SaveChangesAsync();

        // Assert — carga todo con Include para verificar las relaciones
        var leida = await db.Denuncias
            .AsNoTracking()
            .Include(d => d.Denunciado)
            .Include(d => d.Evidencias)
            .SingleAsync(d => d.IdDenuncia == denuncia.IdDenuncia);

        leida.Estado.Should().Be(EstadoDenuncia.Pendiente);
        leida.Descripcion.Should().Be("Fue agredida por su pareja.");
        leida.LatHecho.Should().Be(-13.1587m);
        leida.LngHecho.Should().Be(-74.2237m);
        leida.Denunciado.Should().NotBeNull();
        leida.Denunciado!.NombreAlias.Should().Be("Juan Pérez");
        leida.Denunciado.RelacionVictima.Should().Be(RelacionVictima.Pareja);
        leida.Evidencias.Should().HaveCount(2);
        leida.Evidencias.Should().Contain(e => e.TipoArchivo == TipoArchivo.Imagen);
        leida.Evidencias.Should().Contain(e => e.TipoArchivo == TipoArchivo.Video);
    }

    [Fact]
    public async Task Cambiar_estado_de_denuncia_debe_persistirse_como_string_no_como_numero()
    {
        // Detalle específico de nuestra config EF: los enums se serializan
        // como string ("pendiente", "atendida") para que la BD sea legible.
        using var db = await _fixture.CrearDbContextAsync();
        var victima = Victima.Crear("Ana", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        var denuncia = Denuncia.CrearFormal(victima.IdVictima, "/dni.jpg",
            null, null, null, null, null, null, null, null, "Descripcion.");
        db.Denuncias.Add(denuncia);
        await db.SaveChangesAsync();

        denuncia.CambiarEstado(EstadoDenuncia.Atendida);
        await db.SaveChangesAsync();

        var releida = await db.Denuncias.AsNoTracking()
            .SingleAsync(d => d.IdDenuncia == denuncia.IdDenuncia);
        releida.Estado.Should().Be(EstadoDenuncia.Atendida);
    }

    [Fact]
    public async Task Coordenadas_GPS_debe_conservar_precision_decimal_hasta_7_lugares()
    {
        // La precisión decimal(9,7) para lat y decimal(10,7) para lng permite ~1 cm.
        // Este test verifica que PostgreSQL respeta esa precisión al ida y vuelta.
        using var db = await _fixture.CrearDbContextAsync();
        var victima = Victima.Crear("Ana", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        var denuncia = Denuncia.CrearFormal(victima.IdVictima, "/dni.jpg",
            null, null, null, null,
            lat: -13.1587234m, lng: -74.2237891m,
            null, null, "Descripcion.");
        db.Denuncias.Add(denuncia);
        await db.SaveChangesAsync();

        var releida = await db.Denuncias.AsNoTracking()
            .SingleAsync(d => d.IdDenuncia == denuncia.IdDenuncia);

        releida.LatHecho.Should().Be(-13.1587234m);
        releida.LngHecho.Should().Be(-74.2237891m);
    }
}
