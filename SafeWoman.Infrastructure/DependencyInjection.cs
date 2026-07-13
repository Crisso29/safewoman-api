using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Infrastructure.Services.Admin;
using SafeWoman.Domain.Interfaces;
using SafeWoman.Infrastructure.Persistence;
using SafeWoman.Infrastructure.Persistence.Repositories;
using SafeWoman.Infrastructure.Services.Cleanup;
using SafeWoman.Infrastructure.Services.Realtime;
using SafeWoman.Infrastructure.Services.Security;
using SafeWoman.Infrastructure.Services.Sms;
using SafeWoman.Infrastructure.Services.Storage;
using SafeWoman.Infrastructure.Services.Geocoding;

namespace SafeWoman.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Persistencia ─────────────────────────────────────────────────────────
        // PostgreSQL vía Npgsql. La migración desde SQL Server fue transparente:
        // los tipos EF (int, string, decimal, DateTime, bool) tienen equivalencia
        // directa; las Fluent API configurations son agnósticas del proveedor.
        services.AddDbContext<SafeWomanDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                npg => npg.MigrationsAssembly(typeof(SafeWomanDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IRepository<Victima>,            Repository<Victima>>();
        services.AddScoped<IRepository<OtpVerificacion>,    Repository<OtpVerificacion>>();
        services.AddScoped<IRepository<ContactoEmergencia>, Repository<ContactoEmergencia>>();
        services.AddScoped<IRepository<AlertaSos>,          Repository<AlertaSos>>();
        services.AddScoped<IRepository<Denuncia>,           Repository<Denuncia>>();
        services.AddScoped<IRepository<Denunciado>,         Repository<Denunciado>>();
        services.AddScoped<IRepository<Evidencia>,          Repository<Evidencia>>();
        services.AddScoped<IRepository<HuellaDispositivo>,  Repository<HuellaDispositivo>>();
        services.AddScoped<IRepository<DenunciaAnonima>,    Repository<DenunciaAnonima>>();
        services.AddScoped<IRepository<DenunciadoAnonima>,  Repository<DenunciadoAnonima>>();
        services.AddScoped<IRepository<EvidenciaAnonima>,   Repository<EvidenciaAnonima>>();
        services.AddScoped<IRepository<Administrador>,      Repository<Administrador>>();
        services.AddScoped<IRepository<LogAuditoria>,       Repository<LogAuditoria>>();

        // ── Seguridad ─────────────────────────────────────────────────────────────
        services.AddScoped<IPasswordHasher,    BcryptPasswordHasher>();
        services.AddScoped<ITokenService,      JwtTokenService>();
        services.AddScoped<IOtpCodeGenerator,  OtpCodeGenerator>();

        // Selector del proveedor de SMS por configuración:
        //   "Sms:Provider" = "Twilio"  → SMS reales (cuesta ~$0.08/mensaje a Perú)
        //   "Sms:Provider" = "Console" → escribe en consola, cero costo (default en dev)
        //
        // Si la clave no está en appsettings, en Development usamos Console por seguridad
        // del saldo Twilio. En Production siempre usamos Twilio real.
        var smsProvider = config["Sms:Provider"];
        var usarTwilio = string.Equals(smsProvider, "Twilio", StringComparison.OrdinalIgnoreCase);

        if (usarTwilio)
        {
            services.AddSingleton<TwilioSmsSender>();
            services.AddSingleton<IOtpSender>(sp      => sp.GetRequiredService<TwilioSmsSender>());
            services.AddSingleton<ISosSmsNotifier>(sp => sp.GetRequiredService<TwilioSmsSender>());
        }
        else
        {
            services.AddSingleton<ConsoleSmsSender>();
            services.AddSingleton<IOtpSender>(sp      => sp.GetRequiredService<ConsoleSmsSender>());
            services.AddSingleton<ISosSmsNotifier>(sp => sp.GetRequiredService<ConsoleSmsSender>());
        }

        // Reverse-geocoding para SMS SOS (Nominatim/OSM, gratis).
        services.AddSingleton<IReverseGeocoder, NominatimReverseGeocoder>();

        // ── Almacenamiento y tiempo real ──────────────────────────────────────────
        // DbFileStorage guarda los archivos en PostgreSQL como bytea → sobrevive
        // los redeploys de Render Free (filesystem efímero). LocalFileStorage
        // queda como fallback histórico si algún día se migra a disco persistente.
        services.AddScoped<IFileStorage,  DbFileStorage>();
        services.AddScoped<ISosNotifier,  SignalRSosNotifier>();

        services.AddSignalR();

        // ── Servicios del panel Admin ─────────────────────────────────────────
        services.AddScoped<IAdminService, AdminService>();

        // ── Background services ──────────────────────────────────────────────
        // Purga automática de cuentas no verificadas (defaults: cada 15 min,
        // borra cuentas > 1 h sin verificar). Configurable en appsettings via
        // Cleanup:IntervalMinutes y Cleanup:UnverifiedAccountsHours.
        services.AddHostedService<UnverifiedAccountsCleanupService>();

        return services;
    }
}
