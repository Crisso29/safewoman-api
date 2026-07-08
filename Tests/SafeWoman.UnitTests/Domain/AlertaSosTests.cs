using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// Alerta SOS es el corazón de la funcionalidad de emergencia. Sus transiciones
/// de estado son críticas y deben ser rigurosamente probadas.
/// </summary>
public class AlertaSosTests
{
    [Fact]
    public void Activar_debe_crear_alerta_en_estado_Activa_con_ubicacion_y_timestamp()
    {
        var alerta = AlertaSos.Activar(idVictima: 42, latitud: -13.16m, longitud: -74.22m);

        alerta.IdVictima.Should().Be(42);
        alerta.Latitud.Should().Be(-13.16m);
        alerta.Longitud.Should().Be(-74.22m);
        alerta.Estado.Should().Be(EstadoAlerta.Activa);
        alerta.TimestampActivacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        alerta.TimestampCancelacion.Should().BeNull();
    }

    [Fact]
    public void Cancelar_una_alerta_activa_debe_cambiar_estado_y_registrar_timestamp()
    {
        var alerta = AlertaSos.Activar(1, -13.16m, -74.22m);

        alerta.Cancelar();

        alerta.Estado.Should().Be(EstadoAlerta.Cancelada);
        alerta.TimestampCancelacion.Should().NotBeNull()
            .And.Subject.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Atender_una_alerta_activa_debe_cambiarla_a_Atendida()
    {
        var alerta = AlertaSos.Activar(1, -13.16m, -74.22m);

        alerta.Atender();

        alerta.Estado.Should().Be(EstadoAlerta.Atendida);
        alerta.TimestampCancelacion.Should().NotBeNull(
            "el timestamp de cancelación también se usa cuando la alerta es atendida");
    }

    [Fact]
    public void Cancelar_una_alerta_ya_cancelada_debe_lanzar_DomainException()
    {
        var alerta = AlertaSos.Activar(1, -13.16m, -74.22m);
        alerta.Cancelar();

        var act = () => alerta.Cancelar();

        act.Should().Throw<DomainException>()
           .WithMessage("*solo se puede cancelar una alerta activa*",
               because: "prevenir doble cancelación");
    }

    [Fact]
    public void Atender_una_alerta_ya_cancelada_debe_lanzar_DomainException()
    {
        var alerta = AlertaSos.Activar(1, -13.16m, -74.22m);
        alerta.Cancelar();

        var act = () => alerta.Atender();

        act.Should().Throw<DomainException>()
           .WithMessage("*solo se puede atender una alerta activa*");
    }

    [Fact]
    public void Atender_una_alerta_ya_atendida_debe_lanzar_DomainException()
    {
        // Escenario: dos operadores del panel Admin dan clic simultáneo en "atender".
        var alerta = AlertaSos.Activar(1, -13.16m, -74.22m);
        alerta.Atender();

        var act = () => alerta.Atender();

        act.Should().Throw<DomainException>();
    }
}
