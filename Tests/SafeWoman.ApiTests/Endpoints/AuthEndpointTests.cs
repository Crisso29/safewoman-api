using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafeWoman.ApiTests.Fixtures;
using SafeWoman.Application.DTOs.Auth;
using SafeWoman.Infrastructure.Persistence;

namespace SafeWoman.ApiTests.Endpoints;

/// <summary>
/// Tests del flujo de autenticación completo: registro → verificar OTP → login.
/// Prueba el pipeline COMPLETO: HTTP → controllers → services → EF Core → PostgreSQL.
///
/// Los SMS no se envían de verdad (Sms:Provider=Console en la factory).
/// </summary>
[Collection("Api")]
public class AuthEndpointTests
{
    private readonly SafeWomanApiFactory _factory;
    private static readonly JsonSerializerOptions JsonCase = new(JsonSerializerDefaults.Web);

    public AuthEndpointTests(SafeWomanApiFactory factory)
    {
        _factory = factory;
        LimpiarBaseDatos().GetAwaiter().GetResult();
    }

    // ── REGISTRO ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_registro_con_datos_validos_debe_devolver_200_e_idVictima()
    {
        var client = _factory.CreateClient();
        var body   = new RegistroRequest("Ana Prueba", "12345678", "987654321", "Password123!");

        var response = await client.PostAsJsonAsync("/api/auth/registro", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("idVictima");
        json.Should().Contain("Cuenta creada");
    }

    [Fact]
    public async Task POST_registro_con_DNI_invalido_debe_devolver_400()
    {
        var client = _factory.CreateClient();
        var body   = new RegistroRequest("Ana", "1234", "987654321", "Password123!");  // DNI muy corto

        var response = await client.PostAsJsonAsync("/api/auth/registro", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_registro_con_telefono_invalido_debe_devolver_400()
    {
        var client = _factory.CreateClient();
        var body   = new RegistroRequest("Ana", "12345678", "999", "Password123!");  // teléfono corto

        var response = await client.PostAsJsonAsync("/api/auth/registro", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_registro_con_DNI_duplicado_debe_devolver_400()
    {
        var client = _factory.CreateClient();
        var body1  = new RegistroRequest("Ana", "12345678", "987654321", "Password123!");
        var body2  = new RegistroRequest("Bea", "12345678", "988888888", "Password123!");

        var r1 = await client.PostAsJsonAsync("/api/auth/registro", body1);
        r1.EnsureSuccessStatusCode();
        var r2 = await client.PostAsJsonAsync("/api/auth/registro", body2);

        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "no debe permitirse registrar dos víctimas con el mismo DNI");
    }

    [Fact]
    public async Task POST_registro_con_body_vacio_debe_rechazar_la_peticion()
    {
        // Enviamos un body JSON vacío. ASP.NET Core puede responder 400
        // (validación) o 415 (media type) según el pipeline — cualquiera
        // de los dos códigos indica un rechazo correcto.
        var client = _factory.CreateClient();
        var contenido = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/registro", contenido);

        response.IsSuccessStatusCode.Should().BeFalse(
            "un body vacío debe ser rechazado por los validadores");
    }

    // ── VERIFICAR OTP + LOGIN (flujo completo) ────────────────────────────────

    [Fact]
    public async Task Flujo_completo_registro_verificar_login_debe_devolver_JWT_valido()
    {
        var client = _factory.CreateClient();

        // 1. Registrar
        var regBody = new RegistroRequest("Ana Flujo", "22333444", "911222333", "Password123!");
        var regResp = await client.PostAsJsonAsync("/api/auth/registro", regBody);
        regResp.EnsureSuccessStatusCode();

        // 2. Como el SMS no llega (modo Console), obtengo el OTP directamente de la BD
        var otpCode = await LeerOtpDirectoDeBd("911222333");
        otpCode.Should().NotBeNullOrEmpty();

        // 3. Verificar OTP → debe emitir JWT
        var verifyResp = await client.PostAsJsonAsync("/api/auth/verificar-otp",
            new VerificarOtpRequest("911222333", otpCode!));
        verifyResp.EnsureSuccessStatusCode();

        var authJson = await verifyResp.Content.ReadFromJsonAsync<AuthResponse>(JsonCase);
        authJson.Should().NotBeNull();
        authJson!.Token.Should().NotBeNullOrEmpty("debe emitir JWT tras verificación");
        authJson.Verificada.Should().BeTrue();

        // 4. Login con la misma cuenta ya verificada
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("911222333", "Password123!"));
        loginResp.EnsureSuccessStatusCode();

        var loginAuth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonCase);
        loginAuth!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_login_con_password_incorrecta_debe_devolver_error()
    {
        var client = _factory.CreateClient();

        // Registrar + verificar primero
        var telefono = "922333444";
        var reg = new RegistroRequest("Ana Log", "33445566", telefono, "Password123!");
        await client.PostAsJsonAsync("/api/auth/registro", reg);
        var otp = await LeerOtpDirectoDeBd(telefono);
        await client.PostAsJsonAsync("/api/auth/verificar-otp", new VerificarOtpRequest(telefono, otp!));

        // Login con password incorrecta — puede devolver 400 (credenciales) o
        // 429 (rate limit) si el resto de tests ya consumió el quota. Ambos
        // son rechazos válidos — lo que NUNCA debe pasar es un 200.
        var badLogin = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(telefono, "PasswordMala"));

        badLogin.IsSuccessStatusCode.Should().BeFalse(
            "una password incorrecta jamás debe autenticar");
    }

    [Fact]
    public async Task POST_login_de_cuenta_no_verificada_debe_devolver_400()
    {
        var client = _factory.CreateClient();

        // Registrar SIN verificar OTP
        var telefono = "944555666";
        await client.PostAsJsonAsync("/api/auth/registro",
            new RegistroRequest("Ana", "44556677", telefono, "Password123!"));

        // Intentar login antes de verificar
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(telefono, "Password123!"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    private async Task<string?> LeerOtpDirectoDeBd(string telefono)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeWomanDbContext>();

        var otp = await db.OtpVerificaciones
            .AsNoTracking()
            .Include(o => o.Victima)
            .Where(o => o.Victima.Telefono == telefono && !o.Usado)
            .OrderByDescending(o => o.FechaGeneracion)
            .FirstOrDefaultAsync();

        return otp?.Codigo;
    }

    private async Task LimpiarBaseDatos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeWomanDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
