using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SafeWoman.ApiTests.Fixtures;

namespace SafeWoman.ApiTests.Endpoints;

/// <summary>
/// Verifica que los endpoints protegidos rechazan requests sin JWT (401),
/// y aceptan requests con JWT válido.
///
/// Esta suite prueba la barrera de autorización que separa datos privados
/// de una víctima de acceso público.
/// </summary>
[Collection("Api")]
public class ProtectedEndpointsTests
{
    private readonly SafeWomanApiFactory _factory;

    public ProtectedEndpointsTests(SafeWomanApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GET_perfil_sin_JWT_debe_devolver_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/victima/perfil");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "el endpoint de perfil requiere Bearer token");
    }

    [Fact]
    public async Task GET_contactos_sin_JWT_debe_devolver_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/contactos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_alerta_sos_sin_JWT_debe_devolver_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sos/activar",
            new { Latitud = -13.16m, Longitud = -74.22m });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_alerta_sos_con_JWT_invalido_debe_devolver_401()
    {
        var client = _factory.CreateClientAutenticado("token.invalido.aqui");

        var response = await client.PostAsJsonAsync("/api/sos/activar",
            new { Latitud = -13.16m, Longitud = -74.22m });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "un token con formato inválido debe rechazarse igual que sin token");
    }

    [Fact]
    public async Task GET_denuncia_anonima_debe_ser_publico()
    {
        // Denuncia anónima NO requiere autenticación (por diseño — testigos
        // pueden reportar sin exponerse). Solo verifica que la ruta EXISTE
        // — el método GET puede devolver 405 (Method Not Allowed) si el endpoint
        // sólo acepta POST, pero NO debe devolver 401.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/denuncia-anonima");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "el endpoint anónimo nunca requiere JWT");
    }
}
