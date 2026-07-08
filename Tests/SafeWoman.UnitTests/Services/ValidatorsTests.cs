using FluentAssertions;
using SafeWoman.Application.DTOs.AlertaSos;
using SafeWoman.Application.DTOs.Auth;
using SafeWoman.Application.Validators;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// Tests unitarios de los 4 validadores FluentValidation.
/// Verifican que las reglas de negocio de entrada (formato DNI, teléfono, longitud
/// de password, coordenadas GPS válidas) se aplican correctamente ANTES de que
/// los datos lleguen al servicio.
/// </summary>
public class RegistroRequestValidatorTests
{
    private readonly RegistroRequestValidator _v = new();

    [Fact]
    public void Datos_completos_correctos_deben_pasar_validacion()
    {
        var req = new RegistroRequest("Ana García Prueba", "12345678", "987654321", "PasswordFuerte123");

        _v.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]     // 7 dígitos
    [InlineData("123456789")]   // 9 dígitos
    [InlineData("1234567A")]    // con letra
    public void DNI_invalido_debe_fallar_validacion(string dni)
    {
        var req = new RegistroRequest("Ana", dni, "987654321", "Password123");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Dni));
    }

    [Theory]
    [InlineData("")]
    [InlineData("98765432")]    // 8 dígitos
    [InlineData("9876543210")]  // 10 dígitos
    [InlineData("98765432A")]   // con letra
    public void Telefono_invalido_debe_fallar_validacion(string tel)
    {
        var req = new RegistroRequest("Ana", "12345678", tel, "Password123");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Telefono));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]     // menor a 8 chars
    public void Password_muy_corta_debe_fallar_validacion(string password)
    {
        var req = new RegistroRequest("Ana", "12345678", "987654321", password);

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Password));
    }

    [Fact]
    public void Nombre_vacio_debe_fallar_validacion()
    {
        var req = new RegistroRequest("", "12345678", "987654321", "Password123");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.NombreCompleto));
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _v = new();

    [Theory]
    [InlineData("12345678")]    // DNI válido
    [InlineData("987654321")]   // teléfono válido
    public void Identificador_valido_como_DNI_o_telefono_debe_pasar(string id)
    {
        var req = new LoginRequest(id, "cualquier-pass");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().NotContain(e => e.PropertyName == nameof(req.Identificador));
    }

    [Theory]
    [InlineData("1234567")]      // 7 dígitos
    [InlineData("1234567890")]   // 10 dígitos
    [InlineData("abcdefgh")]     // letras
    public void Identificador_con_longitud_o_caracteres_invalidos_debe_fallar(string id)
    {
        var req = new LoginRequest(id, "pass");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Identificador));
    }

    [Fact]
    public void Password_vacia_debe_fallar_validacion()
    {
        var req = new LoginRequest("12345678", "");

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Password));
    }
}

public class VerificarOtpRequestValidatorTests
{
    private readonly VerificarOtpRequestValidator _v = new();

    [Fact]
    public void Datos_correctos_deben_pasar_validacion()
    {
        var req = new VerificarOtpRequest("987654321", "123456");

        _v.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]    // 5 dígitos
    [InlineData("1234567")]  // 7 dígitos
    [InlineData("12345A")]   // con letra
    public void Codigo_OTP_con_formato_invalido_debe_fallar(string codigo)
    {
        var req = new VerificarOtpRequest("987654321", codigo);

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Codigo));
    }
}

public class ActivarSosRequestValidatorTests
{
    private readonly ActivarSosRequestValidator _v = new();

    [Fact]
    public void Coordenadas_de_Ayacucho_deben_pasar_validacion()
    {
        var req = new ActivarSosRequest(-13.16m, -74.22m);

        _v.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-91)]  // menor a -90
    [InlineData(91)]   // mayor a 90
    public void Latitud_fuera_de_rango_debe_fallar_validacion(decimal lat)
    {
        var req = new ActivarSosRequest(lat, -74.22m);

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Latitud));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitud_fuera_de_rango_debe_fallar_validacion(decimal lng)
    {
        var req = new ActivarSosRequest(-13.16m, lng);

        var resultado = _v.Validate(req);

        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(req.Longitud));
    }
}
