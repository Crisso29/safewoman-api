using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// La verificación OTP es la barrera entre un registro fraudulento y una
/// víctima real. Sus reglas — validez temporal, no reutilización — deben
/// probarse exhaustivamente.
/// </summary>
public class OtpVerificacionTests
{
    [Fact]
    public void Crear_debe_inicializar_no_usado_y_con_expiracion_5_minutos_por_defecto()
    {
        var otp = OtpVerificacion.Crear(idVictima: 1, codigo: "384291");

        otp.IdVictima.Should().Be(1);
        otp.Codigo.Should().Be("384291");
        otp.Usado.Should().BeFalse();
        otp.FechaExpiracion.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(5),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Crear_debe_aceptar_ventana_de_validez_personalizada()
    {
        var otp = OtpVerificacion.Crear(1, "123456", minutosValidez: 15);

        otp.FechaExpiracion.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(15),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void EsValido_debe_devolver_true_para_codigo_correcto_no_expirado()
    {
        var otp = OtpVerificacion.Crear(1, "384291");

        otp.EsValido("384291").Should().BeTrue();
    }

    [Fact]
    public void EsValido_debe_devolver_false_para_codigo_incorrecto()
    {
        var otp = OtpVerificacion.Crear(1, "384291");

        otp.EsValido("000000").Should().BeFalse();
        otp.EsValido("384290").Should().BeFalse();
    }

    [Fact]
    public void EsValido_debe_devolver_false_para_OTP_ya_usado()
    {
        var otp = OtpVerificacion.Crear(1, "384291");
        otp.Consumir();

        otp.EsValido("384291").Should().BeFalse(
            "un OTP debe usarse una sola vez — previene ataques de replay");
    }

    [Fact]
    public void EsValido_debe_devolver_false_para_OTP_expirado()
    {
        // Genera un OTP con -1 minuto de validez → nace ya expirado.
        var otp = OtpVerificacion.Crear(1, "384291", minutosValidez: -1);

        otp.EsValido("384291").Should().BeFalse(
            "un OTP expirado no puede seguir sirviendo");
    }

    [Fact]
    public void Consumir_debe_marcar_como_usado()
    {
        var otp = OtpVerificacion.Crear(1, "384291");
        otp.Usado.Should().BeFalse();

        otp.Consumir();

        otp.Usado.Should().BeTrue();
    }

    [Fact]
    public void Consumir_un_OTP_ya_usado_debe_lanzar_DomainException()
    {
        var otp = OtpVerificacion.Crear(1, "384291");
        otp.Consumir();

        var act = () => otp.Consumir();

        act.Should().Throw<DomainException>()
           .WithMessage("*ya fue utilizado*");
    }
}
