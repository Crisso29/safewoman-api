using System.Text.Json;
using SafeWoman.Services;

namespace SafeWoman;

public partial class App : Application
{
    private readonly AuthStateService _authState;

    public App(AppShell shell, AuthStateService authState)
    {
        InitializeComponent();
        // Forzamos tema Light siempre. Si el sistema está en modo oscuro, los Entry/Editor
        // sin TextColor explícito heredan el color del sistema (texto blanco) sobre nuestro
        // fondo blanco → texto invisible. Con Light forzado, todos los inputs se ven correctos
        // sin tener que setear TextColor en cada uno.
        UserAppTheme = AppTheme.Light;

        _authState = authState;
        MainPage   = shell;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        try
        {
            // Pre-navegación rápida: si hay bandera en Preferences (sin tocar Keystore)
            // saltar directo a HomePage antes de verificar el token real, para eliminar
            // el flash de WelcomePage tras un reinicio por proceso-kill de Android.
            if (_authState.ProbablyLoggedIn && Shell.Current is not null)
                await Shell.Current.GoToAsync("//HomePage", animate: false);

            // Verificación real del token en background (Keystore puede tardar 50-500 ms).
            await _authState.InitAsync();

            if (_authState.HasToken && !IsTokenExpired(_authState.CachedToken))
            {
                var loc = Shell.Current?.CurrentState?.Location?.ToString() ?? "";
                if (!loc.Contains("HomePage") && Shell.Current is not null)
                    await Shell.Current.GoToAsync("//HomePage", animate: false);
            }
            else if (_authState.HasToken)
            {
                _authState.ClearSession();
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//WelcomePage", animate: false);
            }
            else if (_authState.ProbablyLoggedIn && Shell.Current is not null)
            {
                // La bandera decía "logueado" pero el Keystore ya no tiene token
                // (por ejemplo, el usuario limpió datos): volver a WelcomePage.
                _authState.ClearSession();
                await Shell.Current.GoToAsync("//WelcomePage", animate: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App.OnStart] {ex.Message}");
        }
    }

    protected override async void OnResume()
    {
        base.OnResume();
        // Si el token expiró mientras la app estaba en background, forzar re-login
        if (_authState.HasToken && IsTokenExpired(_authState.CachedToken))
        {
            _authState.ClearSession();
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//WelcomePage", animate: false);
        }
    }

    private static bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token)) return true;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;

            var payload = parts[1];
            payload += new string('=', (4 - payload.Length % 4) % 4);
            payload  = payload.Replace('-', '+').Replace('_', '/');

            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!doc.RootElement.TryGetProperty("exp", out var expEl)) return true;

            return DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64()).UtcDateTime < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
