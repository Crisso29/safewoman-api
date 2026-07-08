using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// Verifica las reglas de negocio de la denuncia formal — el corazón del sistema
/// legal. Cualquier defecto aquí puede invalidar denuncias reales.
/// </summary>
public class DenunciaTests
{
    [Fact]
    public void CrearFormal_debe_inicializar_estado_Pendiente_y_marcar_declaracion_jurada()
    {
        var denuncia = DenunciaFormalDeEjemplo();

        denuncia.Estado.Should().Be(EstadoDenuncia.Pendiente);
        denuncia.Tipo.Should().Be(TipoDenuncia.Formal);
        denuncia.DeclaracionJurada.Should().BeTrue("firmar denuncia formal implica declaración jurada");
        denuncia.FechaEnvio.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CrearFormal_debe_conservar_todos_los_datos_de_ubicacion_del_hecho()
    {
        var denuncia = Denuncia.CrearFormal(
            idVictima: 42,
            fotoDniRuta: "/uploads/dni.jpg",
            departamento: "Ayacucho",
            provincia: "Huamanga",
            distrito: "Ayacucho",
            referenciaUbicacion: "Av. Ramón Castilla 234",
            lat: -13.1587m,
            lng: -74.2237m,
            fechaHecho: new DateOnly(2026, 7, 1),
            horaHecho: new TimeOnly(23, 30),
            descripcion: "Fue agredida verbal y físicamente por su pareja.");

        denuncia.IdVictima.Should().Be(42);
        denuncia.FotoDniRuta.Should().Be("/uploads/dni.jpg");
        denuncia.Departamento.Should().Be("Ayacucho");
        denuncia.Provincia.Should().Be("Huamanga");
        denuncia.Distrito.Should().Be("Ayacucho");
        denuncia.ReferenciaUbicacion.Should().Be("Av. Ramón Castilla 234");
        denuncia.LatHecho.Should().Be(-13.1587m);
        denuncia.LngHecho.Should().Be(-74.2237m);
        denuncia.FechaHecho.Should().Be(new DateOnly(2026, 7, 1));
        denuncia.HoraHecho.Should().Be(new TimeOnly(23, 30));
        denuncia.Descripcion.Should().StartWith("Fue agredida");
    }

    [Fact]
    public void CambiarEstado_debe_actualizar_el_estado_de_la_denuncia()
    {
        var denuncia = DenunciaFormalDeEjemplo();

        denuncia.CambiarEstado(EstadoDenuncia.EnProceso);
        denuncia.Estado.Should().Be(EstadoDenuncia.EnProceso);

        denuncia.CambiarEstado(EstadoDenuncia.Atendida);
        denuncia.Estado.Should().Be(EstadoDenuncia.Atendida);
    }

    [Fact]
    public void CambiarEstado_al_mismo_estado_debe_ser_idempotente()
    {
        var denuncia = DenunciaFormalDeEjemplo();
        var estadoInicial = denuncia.Estado;

        denuncia.CambiarEstado(estadoInicial);

        denuncia.Estado.Should().Be(estadoInicial);
    }

    [Fact]
    public void Evidencias_debe_estar_inicializada_como_coleccion_vacia()
    {
        var denuncia = DenunciaFormalDeEjemplo();

        denuncia.Evidencias.Should().NotBeNull().And.BeEmpty();
    }

    private static Denuncia DenunciaFormalDeEjemplo() =>
        Denuncia.CrearFormal(
            idVictima: 1,
            fotoDniRuta: "/uploads/dni.jpg",
            departamento: "Ayacucho",
            provincia: "Huamanga",
            distrito: "Ayacucho",
            referenciaUbicacion: "Plaza Mayor",
            lat: -13.16m,
            lng: -74.22m,
            fechaHecho: DateOnly.FromDateTime(DateTime.Today),
            horaHecho: TimeOnly.FromDateTime(DateTime.Now),
            descripcion: "Descripción del hecho.");
}
