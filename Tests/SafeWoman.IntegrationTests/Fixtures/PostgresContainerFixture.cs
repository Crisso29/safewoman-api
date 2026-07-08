using Microsoft.EntityFrameworkCore;
using SafeWoman.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SafeWoman.IntegrationTests.Fixtures;

/// <summary>
/// Fixture compartido que levanta UN contenedor PostgreSQL para toda la colección
/// de tests de integración. La imagen se descarga la primera vez (~30s) y el
/// contenedor arranca en ~5s en las siguientes ejecuciones.
///
/// Al finalizar la colección, el contenedor se destruye automáticamente.
///
/// Cada test recibe una BD limpia porque llamamos a EnsureDeleted + EnsureCreated
/// al inicio de cada uno (ver <see cref="CrearDbContextAsync"/>).
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    // Imagen Alpine — pesa ~90MB en vez de los 450MB de la imagen full.
    // Compatible con PostgreSQL 16 (misma versión que producción en Neon).
    private const string PostgresImage = "postgres:16-alpine";

    public PostgreSqlContainer Container { get; }

    public PostgresContainerFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("safewoman_test")
            .WithUsername("test_user")
            .WithPassword("test_pass_2026")
            .WithCleanUp(true)  // Elimina el contenedor al terminar los tests
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Docker Desktop debe estar corriendo — si falla aquí, el mensaje dice cómo abrirlo.
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    /// <summary>
    /// Devuelve un DbContext apuntando al contenedor con las migraciones aplicadas.
    /// Cada llamada elimina y recrea la BD → tests 100% aislados entre sí.
    /// </summary>
    public async Task<SafeWomanDbContext> CrearDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<SafeWomanDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;

        var db = new SafeWomanDbContext(options);

        // BD limpia por test: destruye y recrea con el modelo actual.
        // EnsureCreated aplica el esquema derivado del DbContext (no las migraciones EF).
        // Es más rápido que Migrate() y suficiente para tests de integración.
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        return db;
    }
}

/// <summary>
/// Marca la colección para que xUnit comparta la misma instancia del
/// contenedor entre todos los tests que apliquen [Collection("Postgres")].
/// Sin esta colección, cada test crearía SU propio contenedor (lento y costoso).
/// </summary>
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }
