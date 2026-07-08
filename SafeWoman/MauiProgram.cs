using Microsoft.Extensions.Logging;
using SafeWoman.Services;
using SafeWoman.ViewModels.Auth;
using SafeWoman.ViewModels.Contacto;
using SafeWoman.ViewModels.Denuncia;
using SafeWoman.ViewModels.Home;
using SafeWoman.ViewModels.Perfil;
using SafeWoman.Views.Auth;
using SafeWoman.Views.Contacto;
using SafeWoman.Views.Denuncia;
using SafeWoman.Views.Home;
using SafeWoman.Views.Perfil;

namespace SafeWoman;

public static class MauiProgram
{
    // ── URL de la API ────────────────────────────────────────────────────────
    // DEV emulador  → 10.0.2.2:7273  (Android Emulator apunta al host)
    // DEV dispositivo → ngrok URL    (teléfono real en cualquier red)
    // RELEASE       → URL del servidor de producción
    // IP del PC en la Wi-Fi (para probar en teléfono FÍSICO conectado a la misma red).
    // Cuando cambies de Wi-Fi o el router te dé otra IP, actualiza aquí.
    // Averigua tu IP corriendo `ipconfig` en cmd → busca "Wi-Fi" → "Dirección IPv4".
    private const string LanIp = "192.168.18.30";

#if DEBUG && ANDROID
    // Usamos HTTP puerto 5015 (no HTTPS) para el teléfono físico.
    // Motivo: Android real rechaza certificados self-signed de dev-certs sin
    // configurar network_security_config, y ese trámite complica la primera prueba.
    // HTTP en la Wi-Fi local, en modo Debug, es aceptable — no expone datos reales.
    // Cuando vayas a producción, la URL Release usará HTTPS con certificado válido.
    //   Emulador  →  "http://10.0.2.2:5015/api/"
    //   Teléfono  →  "http://" + LanIp + ":5015/api/"
    private const string ApiBaseUrl = "http://" + LanIp + ":5015/api/";
#elif DEBUG
    private const string ApiBaseUrl = "https://localhost:7273/api/";
#else
    // ↓ Reemplaza con tu URL ngrok o dominio de producción antes de publicar.
    // Cuando la URL sea correcta, elimina la directiva #error de más abajo.
    private const string ApiBaseUrl = "https://REEMPLAZAR_CON_URL_NGROK/api/";
    #error La URL de la API en RELEASE aún es el placeholder. Reemplaza ApiBaseUrl en MauiProgram.cs con tu URL real y elimina esta directiva #error antes de publicar.
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Handler custom para que el WebView del mapa (Leaflet + CartoDB tiles)
                // pueda cargar recursos HTTPS externos desde su contexto file://.
                handlers.AddHandler(
                    typeof(Microsoft.Maui.Controls.WebView),
                    typeof(SafeWoman.Platforms.Android.CustomWebViewHandler));
#endif
            });

        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AuthStateService>();
        services.AddSingleton<DeviceFingerprintService>();
        services.AddSingleton<LocationService>();
        services.AddSingleton<GeocodingService>();

#if ANDROID
        services.AddSingleton<IAlarmService, SafeWoman.Platforms.Android.AlarmService>();
#else
        services.AddSingleton<IAlarmService, SafeWoman.Platforms.Windows.AlarmService>();
#endif

        // ApiService inyecta el Bearer manualmente en cada request (ver ApiService.BuildRequest).
        // No usamos AddHttpMessageHandler porque el pipeline de HttpClientFactory en MAUI
        // no siempre propaga bien el token del singleton AuthStateService.
        services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri(ApiBaseUrl);
            client.Timeout     = TimeSpan.FromSeconds(100);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
            return handler;
        });
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<OtpViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SosActiveViewModel>();
        services.AddTransient<DenunciaFormalViewModel>();
        services.AddTransient<DenunciaAnonimaViewModel>();
        services.AddTransient<ContactosViewModel>();
        services.AddTransient<PerfilViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<WelcomePage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<OtpPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<SosActivePage>();
        services.AddTransient<DenunciaFormalPage>();
        services.AddTransient<DenunciaAnonimaPage>();
        services.AddTransient<ContactosPage>();
        services.AddTransient<PerfilPage>();

        services.AddSingleton<AppShell>();
    }
}
