using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SafeWoman.Domain.Entities;
using SafeWoman.Infrastructure.Services.Security;

namespace SafeWoman.UnitTests.Security;

/// <summary>
/// Verifica la emisión de tokens JWT — la primitiva que sostiene toda la
/// autenticación móvil. Un bug aquí puede permitir suplantación de identidad.
/// </summary>
public class JwtTokenServiceTests
{
    private readonly IConfiguration _config;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]             = "clave-de-prueba-super-secreta-de-64-caracteres-para-tests-2026!!",
                ["Jwt:Issuer"]          = "SafeWoman.API",
                ["Jwt:Audience"]        = "SafeWoman.Mobile",
                ["Jwt:ExpirationHours"] = "4"
            })
            .Build();

        _sut = new JwtTokenService(_config);
    }

    [Fact]
    public void GenerateVictimaToken_debe_producir_un_token_valido_no_vacio()
    {
        var victima = CrearVictimaEjemplo();

        var token = _sut.GenerateVictimaToken(victima);

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3, "un JWT tiene 3 segmentos: header.payload.signature");
    }

    [Fact]
    public void Token_debe_incluir_el_id_de_la_victima_en_el_claim_sub()
    {
        var victima = CrearVictimaEjemplo();

        var token = _sut.GenerateVictimaToken(victima);
        var decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);

        decoded.Subject.Should().Be(victima.IdVictima.ToString());
    }

    [Fact]
    public void Token_debe_incluir_issuer_y_audience_configurados()
    {
        var victima = CrearVictimaEjemplo();

        var token = _sut.GenerateVictimaToken(victima);
        var decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);

        decoded.Issuer.Should().Be("SafeWoman.API");
        decoded.Audiences.Should().Contain("SafeWoman.Mobile");
    }

    [Fact]
    public void Token_debe_expirar_en_el_tiempo_configurado()
    {
        var victima = CrearVictimaEjemplo();
        var antes   = DateTime.UtcNow;

        var token   = _sut.GenerateVictimaToken(victima);
        var decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Tolerancia de 60 segundos por diferencia de reloj durante el test.
        decoded.ValidTo.Should().BeCloseTo(antes.AddHours(4), TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Token_debe_incluir_claim_de_rol_Victima()
    {
        var victima = CrearVictimaEjemplo();

        var token = _sut.GenerateVictimaToken(victima);
        var decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);

        decoded.Claims.Should().Contain(c =>
            c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase) && c.Value == "Victima");
    }

    [Fact]
    public void Tokens_del_mismo_usuario_deben_ser_distintos_por_jti_aleatorio()
    {
        // Cada token tiene un jti (JWT ID) único → previene ataques de replay.
        var victima = CrearVictimaEjemplo();

        var t1 = _sut.GenerateVictimaToken(victima);
        var t2 = _sut.GenerateVictimaToken(victima);

        t1.Should().NotBe(t2);
    }

    private static Victima CrearVictimaEjemplo()
    {
        return Victima.Crear(
            nombreCompleto: "Ana Prueba",
            dni: "12345678",
            telefono: "+51987654321",
            passwordHash: "$2a$12$fakeHashParaTesting");
    }
}
