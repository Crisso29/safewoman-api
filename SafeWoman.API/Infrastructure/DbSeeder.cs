using SafeWoman.Application.Services;

namespace SafeWoman.API.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var sp           = scope.ServiceProvider;
        var config       = sp.GetRequiredService<IConfiguration>();
        var adminAuth    = sp.GetRequiredService<AdminAuthService>();
        var logger       = sp.GetRequiredService<ILogger<Program>>();

        var seed = config.GetSection("AdminSeed");
        var email    = seed["Email"];
        var password = seed["Password"];
        var nombre   = seed["Nombre"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;

        try
        {
            if (!await adminAuth.ExisteAdminAsync())
            {
                await adminAuth.CrearAdminAsync(nombre ?? "Admin", email, password);
                logger.LogInformation("Admin semilla creado: {Email}", email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear el admin semilla");
        }
    }
}
