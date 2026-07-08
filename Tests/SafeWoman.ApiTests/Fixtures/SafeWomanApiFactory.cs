using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeWoman.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SafeWoman.ApiTests.Fixtures;

/// <summary>
/// Fábrica personalizada que arranca la API completa en memoria pero sustituye:
///  - La cadena de conexión → apunta al contenedor PostgreSQL efímero.
///  - El proveedor SMS      → "Console" (no consume saldo Twilio).
///  - Las credenciales JWT  → clave conocida solo por los tests.
///  - El admin seed         → credenciales predecibles para tests.
///
/// El pipeline ASP.NET Core (middlewares, controllers, DI, auth, SignalR) corre
/// exactamente igual que en producción. Solo cambian las 4 dependencias externas.
///
/// Es la forma correcta de hacer tests de API end-to-end: sin mocks del pipeline,
/// pero con infraestructura efímera y aislada.
/// </summary>
public class SafeWomanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string PostgresImage = "postgres:16-alpine";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage(PostgresImage)
        .WithDatabase("safewoman_api_test")
        .WithUsername("test_user")
        .WithPassword("test_pass_2026")
        .WithCleanUp(true)
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting llega ANTES de que el pipeline lea configuración —
        // necesario para valores como RateLimit que se evalúan en el bootstrap.
        builder.UseSetting("RateLimit:AuthPerMinute", "10000");

        // Sobreescribe la configuración con valores específicos del test.
        // Estos se aplican ANTES de que se resuelva ninguna dependencia.
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Cadena de conexión → contenedor efímero
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),

                // SMS en modo consola: cero costo Twilio, cero llamadas reales
                ["Sms:Provider"] = "Console",

                // JWT — clave estable solo para tests (nunca la de producción)
                ["Jwt:Key"]             = "test-key-super-secreta-de-64-caracteres-para-tests-2026-safewoman-api!",
                ["Jwt:Issuer"]          = "SafeWoman.API",
                ["Jwt:Audience"]        = "SafeWoman.Mobile",
                ["Jwt:ExpirationHours"] = "4",

                // Admin seed — credenciales conocidas para tests del panel
                ["AdminSeed:Email"]    = "admin-test@safewoman.pe",
                ["AdminSeed:Password"] = "Admin-Test-2026!",
                ["AdminSeed:Nombre"]   = "Admin Test",

                // Rate limit alto — los tests hacen decenas de llamadas seguidas
                // desde la misma IP; el límite de producción (10/min) los rompería.
                ["RateLimit:AuthPerMinute"] = "10000"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Reemplaza el DbContext registrado en Program.cs con uno apuntando
            // al contenedor (por si algún day la config no lo pilla vía AppConfiguration).
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<SafeWomanDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<SafeWomanDbContext>(opt =>
                opt.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    /// <summary>
    /// Devuelve un HttpClient con el JWT ya configurado en el header Authorization.
    /// Útil para tests que necesitan un usuario víctima autenticado.
    /// </summary>
    public HttpClient CreateClientAutenticado(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

/// <summary>
/// Marca la colección para compartir la misma factory (y por ende el mismo
/// contenedor PostgreSQL) entre todos los tests de API.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<SafeWomanApiFactory> { }
