using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// Verifica que la denuncia anónima NO vincula la identidad del denunciante —
/// solo la huella del dispositivo, para trazabilidad técnica sin revelar quién.
/// </summary>
public class DenunciaAnonimaTests
{
    [Fact]
    public void Crear_debe_inicializar_pendiente_y_vinculada_solo_a_huella()
    {
        var denuncia = DenunciaAnonima.Crear(
            idHuella: 7,
            departamento: "Ayacucho",
            provincia: "Huamanga",
            distrito: "Ayacucho",
            referenciaUbicacion: "Cerca al mercado",
            lat: -13.16m,
            lng: -74.22m,
            fechaHecho: new DateOnly(2026, 7, 1),
            horaHecho: new TimeOnly(20, 0),
            descripcion: "Testigo presenció agresión.");

        denuncia.IdHuella.Should().Be(7, "solo se guarda huella, no identidad");
        denuncia.Estado.Should().Be(EstadoDenuncia.Pendiente);
        denuncia.FechaEnvio.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_debe_aceptar_ubicacion_sin_coordenadas_GPS()
    {
        // Escenario real: testigo denuncia por texto sin usar GPS.
        var denuncia = DenunciaAnonima.Crear(
            idHuella: 1,
            departamento: "Ayacucho", provincia: null, distrito: null,
            referenciaUbicacion: null,
            lat: null, lng: null,
            fechaHecho: null, horaHecho: null,
            descripcion: "Hecho sin ubicación exacta.");

        denuncia.LatHecho.Should().BeNull();
        denuncia.LngHecho.Should().BeNull();
        denuncia.ReferenciaUbicacion.Should().BeNull();
    }

    [Fact]
    public void CambiarEstado_debe_permitir_el_flujo_pendiente_a_atendida()
    {
        var denuncia = DenunciaAnonima.Crear(1, null, null, null, null, null, null, null, null, null);

        denuncia.CambiarEstado(EstadoDenuncia.EnProceso);
        denuncia.Estado.Should().Be(EstadoDenuncia.EnProceso);

        denuncia.CambiarEstado(EstadoDenuncia.Atendida);
        denuncia.Estado.Should().Be(EstadoDenuncia.Atendida);
    }

    [Fact]
    public void Evidencias_debe_estar_inicializada_vacia()
    {
        var denuncia = DenunciaAnonima.Crear(1, null, null, null, null, null, null, null, null, null);

        denuncia.Evidencias.Should().NotBeNull().And.BeEmpty();
    }
}
