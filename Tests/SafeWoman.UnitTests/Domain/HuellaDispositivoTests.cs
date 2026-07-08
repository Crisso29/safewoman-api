using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// La huella de dispositivo permite trazabilidad técnica de denuncias anónimas
/// (para detectar spam masivo sin revelar identidad). Sus transiciones bloquear/
/// desbloquear son idempotentes y deben validar precondiciones.
/// </summary>
public class HuellaDispositivoTests
{
    [Fact]
    public void Crear_debe_inicializar_huella_no_bloqueada_con_timestamps_actuales()
    {
        var huella = HuellaDispositivo.Crear("abc123def456");

        huella.DeviceFingerprint.Should().Be("abc123def456");
        huella.Bloqueada.Should().BeFalse();
        huella.FechaPrimerUso.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        huella.FechaUltimoUso.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RegistrarUso_debe_actualizar_FechaUltimoUso()
    {
        var huella = HuellaDispositivo.Crear("abc");
        var primerUsoOriginal = huella.FechaPrimerUso;

        Thread.Sleep(10);
        huella.RegistrarUso();

        huella.FechaUltimoUso.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        huella.FechaPrimerUso.Should().Be(primerUsoOriginal,
            "el primer uso jamás debe cambiar tras el registro inicial");
    }

    [Fact]
    public void Bloquear_una_huella_no_bloqueada_debe_cambiar_estado_a_bloqueada()
    {
        var huella = HuellaDispositivo.Crear("abc");

        huella.Bloquear();

        huella.Bloqueada.Should().BeTrue();
    }

    [Fact]
    public void Bloquear_una_huella_ya_bloqueada_debe_lanzar_DomainException()
    {
        var huella = HuellaDispositivo.Crear("abc");
        huella.Bloquear();

        var act = () => huella.Bloquear();

        act.Should().Throw<DomainException>()
           .WithMessage("*ya está bloqueado*");
    }

    [Fact]
    public void Desbloquear_una_huella_bloqueada_debe_reactivarla()
    {
        var huella = HuellaDispositivo.Crear("abc");
        huella.Bloquear();

        huella.Desbloquear();

        huella.Bloqueada.Should().BeFalse();
    }

    [Fact]
    public void Desbloquear_una_huella_no_bloqueada_debe_lanzar_DomainException()
    {
        var huella = HuellaDispositivo.Crear("abc");

        var act = () => huella.Desbloquear();

        act.Should().Throw<DomainException>()
           .WithMessage("*no está bloqueado*");
    }
}
