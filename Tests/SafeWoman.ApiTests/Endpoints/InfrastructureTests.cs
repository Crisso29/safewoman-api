using System.Net;
using FluentAssertions;
using SafeWoman.ApiTests.Fixtures;

namespace SafeWoman.ApiTests.Endpoints;

/// <summary>
/// Tests del pipeline HTTP: verifica rutas públicas, hardening OWASP y
/// que la ruta vieja del panel Admin quedó cerrada.
/// </summary>
[Collection("Api")]
public class InfrastructureTests
{
    private readonly SafeWomanApiFactory _factory;

    public InfrastructureTests(SafeWomanApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GET_root_debe_devolver_200_con_mensaje_neutro()
    {
        // La raíz NO debe revelar la existencia del panel Admin.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SafeWoman");
        body.Should().NotContain("Admin", "la raíz no debe delatar el panel administrativo");
    }

    [Fact]
    public async Task GET_ruta_admin_vieja_debe_devolver_404()
    {
        // /Admin/Auth/Login es el path predecible que scanners buscan.
        // Después del hardening debe responder 404, no redirect ni HTML.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Admin/Auth/Login");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_panel_safewoman_debe_responder_login_form()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/panel-safewoman/Auth/Login");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Todas_las_respuestas_deben_incluir_cabeceras_OWASP()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
        response.Headers.Should().Contain(h => h.Key == "Referrer-Policy");
        response.Headers.Should().Contain(h => h.Key == "Permissions-Policy");
    }

    [Fact]
    public async Task GET_swagger_debe_devolver_documentacion_OpenAPI()
    {
        // Swagger está habilitado también en producción para revisión académica.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SafeWoman API");
    }
}
