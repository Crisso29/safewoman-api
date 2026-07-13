using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeWoman.Infrastructure.Persistence;

namespace SafeWoman.Infrastructure.Services.Cleanup;

/// Elimina automáticamente las cuentas de víctimas que se registraron pero
/// nunca completaron la verificación OTP. Evita que el panel admin y la BD
/// se llenen de "zombis" (usuarias que abandonaron el flujo o se equivocaron
/// de teléfono y quedaron atrapadas sin poder volver a registrarse).
///
/// La cascade configurada en OtpVerificacionConfiguration elimina también los
/// códigos OTP asociados a cada víctima borrada.
public class UnverifiedAccountsCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnverifiedAccountsCleanupService> _logger;
    private readonly TimeSpan _intervalo;
    private readonly TimeSpan _antiguedadMinima;

    public UnverifiedAccountsCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<UnverifiedAccountsCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;

        // Configurable en appsettings; defaults sensatos si no se define:
        //   Cleanup:IntervalMinutes           = cada 15 min corre el barrido
        //   Cleanup:UnverifiedAccountsHours   = elimina cuentas > 1 h sin verificar
        _intervalo        = TimeSpan.FromMinutes(config.GetValue<int?>("Cleanup:IntervalMinutes") ?? 15);
        _antiguedadMinima = TimeSpan.FromHours  (config.GetValue<double?>("Cleanup:UnverifiedAccountsHours") ?? 1.0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Cleanup service iniciado. Intervalo: {Intervalo}, antigüedad mínima: {Antig}",
            _intervalo, _antiguedadMinima);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LimpiarCuentasNoVerificadasAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una excepción no debe tumbar el hosted service — solo la loggeamos
                // y esperamos al siguiente ciclo. La cleanup no es crítica.
                _logger.LogError(ex,
                    "Error en ciclo de cleanup. Se reintentará en {Intervalo}", _intervalo);
            }

            try
            {
                await Task.Delay(_intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Cleanup service detenido.");
    }

    private async Task LimpiarCuentasNoVerificadasAsync(CancellationToken ct)
    {
        // Scope propio porque BackgroundService es Singleton pero el DbContext es Scoped.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeWomanDbContext>();

        var limite = DateTime.UtcNow.Subtract(_antiguedadMinima);

        // IgnoreQueryFilters saltea el filtro global de Victima.Activa: así también
        // limpiamos cuentas desactivadas que además nunca se verificaron.
        // ExecuteDeleteAsync ejecuta un DELETE directo y las FK ON DELETE CASCADE
        // se encargan de eliminar los OTPs asociados.
        var eliminadas = await db.Victimas
            .IgnoreQueryFilters()
            .Where(v => !v.Verificada && v.FechaRegistro < limite)
            .ExecuteDeleteAsync(ct);

        if (eliminadas > 0)
            _logger.LogInformation(
                "Cleanup: {N} cuenta(s) no verificada(s) eliminada(s) (registradas antes de {Limite:o} UTC)",
                eliminadas, limite);
    }
}
