using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafeWoman.Domain.Entities;
using SafeWoman.IntegrationTests.Fixtures;

namespace SafeWoman.IntegrationTests.Persistence;

/// <summary>
/// Tests de integración de la entidad Victima contra PostgreSQL REAL (vía
/// Testcontainers). Detectan bugs específicos del dialecto SQL que EF InMemory
/// jamás encontraría: tipos de columna, timestamps con zona horaria, defaults SQL, etc.
///
/// Cada test recibe una BD limpia — no hay contaminación entre pruebas.
/// </summary>
[Collection("Postgres")]
public class VictimaPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;

    public VictimaPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Persistir_una_victima_y_leerla_debe_conservar_todos_sus_datos()
    {
        // Arrange
        using var db = await _fixture.CrearDbContextAsync();
        var victima  = Victima.Crear("Ana Prueba", "12345678", "987654321", "$2a$12$hash");

        // Act
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        // Assert — releer con AsNoTracking para descartar del change tracker y
        // forzar una nueva query real a la BD.
        var leida = await db.Victimas
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Dni == "12345678");

        leida.Should().NotBeNull();
        leida!.NombreCompleto.Should().Be("Ana Prueba");
        leida.Telefono.Should().Be("987654321");
        leida.Verificada.Should().BeFalse();
        leida.Activa.Should().BeTrue();
    }

    [Fact]
    public async Task FechaRegistro_debe_persistirse_como_UTC_en_PostgreSQL()
    {
        // Test crítico específico de PostgreSQL: los timestamps se guardan y leen
        // en UTC porque las columnas son `timestamp with time zone`.
        using var db = await _fixture.CrearDbContextAsync();
        var antesUtc = DateTime.UtcNow;
        var victima = Victima.Crear("Ana", "22222222", "987654321", "hash");

        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        var leida = await db.Victimas.AsNoTracking().SingleAsync(v => v.Dni == "22222222");

        leida.FechaRegistro.Kind.Should().Be(DateTimeKind.Utc,
            "PostgreSQL debe devolver los timestamps en UTC");
        leida.FechaRegistro.Should().BeCloseTo(antesUtc, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Buscar_por_DNI_inexistente_debe_devolver_null()
    {
        using var db = await _fixture.CrearDbContextAsync();

        var resultado = await db.Victimas.FirstOrDefaultAsync(v => v.Dni == "00000000");

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Marcar_como_verificada_debe_persistir_el_cambio()
    {
        using var db = await _fixture.CrearDbContextAsync();
        var victima  = Victima.Crear("Ana", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        victima.Verificar();
        await db.SaveChangesAsync();

        var releida = await db.Victimas.AsNoTracking().SingleAsync(v => v.IdVictima == victima.IdVictima);
        releida.Verificada.Should().BeTrue();
    }

    [Fact]
    public async Task Multiples_victimas_deben_persistirse_sin_colisionar_por_DNI_similar()
    {
        using var db = await _fixture.CrearDbContextAsync();

        db.Victimas.Add(Victima.Crear("Ana",   "12345678", "999111111", "hash1"));
        db.Victimas.Add(Victima.Crear("Bea",   "12345679", "999222222", "hash2"));
        db.Victimas.Add(Victima.Crear("Clara", "12345680", "999333333", "hash3"));
        await db.SaveChangesAsync();

        (await db.Victimas.CountAsync()).Should().Be(3);
    }

}
