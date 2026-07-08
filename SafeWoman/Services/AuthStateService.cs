using SafeWoman.Models;

namespace SafeWoman.Services;

/// Gestiona el estado de la sesión de la víctima.
///
/// Persistencia dual del token (defensa en profundidad):
///   1. SecureStorage (Keystore Android) — cifrado, más seguro pero puede fallar
///      silenciosamente en algunos dispositivos (Keystore corrupto, permisos, etc.).
///   2. Preferences (SharedPreferences Android) — texto plano, sync, siempre disponible.
///
/// En Debug/Dev privilegiamos disponibilidad sobre confidencialidad. Para producción
/// se puede desactivar la copia en Preferences (setear TokenEnPreferencias=false).
public class AuthStateService
{
    private const string TokenKey       = "jwt_token";
    private const string NombreKey      = "nombre_completo";
    private const string DniKey         = "dni";
    private const string TelefonoKey    = "telefono";
    private const string IdKey          = "id_victima";
    private const string LoggedInKey    = "session_active";
    private const string TokenBackupKey = "jwt_token_backup"; // fallback en Preferences

    // Habilita el guardado del token también en Preferences (sync + robusto).
    // En producción con datos reales, considerar poner false y confiar solo en SecureStorage.
    private const bool TokenEnPreferencias = true;

    private string?           _tokenCache;
    private VictimaPerfilDto? _perfilCache;
    private DateTime          _ultimoLoginUtc = DateTime.MinValue;
    public  DateTime          UltimoLoginUtc => _ultimoLoginUtc;

    public bool ContactosCacheInvalidado { get; set; } = true;

    public string? NombreCompleto => Preferences.Default.Get<string?>(NombreKey, null);
    public string? Dni            => Preferences.Default.Get<string?>(DniKey, null);
    public string? Telefono       => Preferences.Default.Get<string?>(TelefonoKey, null);
    public int     IdVictima      => Preferences.Default.Get(IdKey, 0);

    public VictimaPerfilDto? PerfilCacheado => _perfilCache;
    public void GuardarPerfil(VictimaPerfilDto perfil) => _perfilCache = perfil;

    public void InvalidarPerfil()
    {
        _perfilCache = null;
        ContactosCacheInvalidado = true;
    }

    /// Devuelve el token de la sesión. Orden de lectura:
    ///   1. Cache en memoria (más rápido).
    ///   2. Preferences (sync, siempre disponible en el hilo actual).
    ///   3. SecureStorage (async, puede fallar en Android — último recurso).
    /// Actualiza la cache en cualquiera de los casos.
    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_tokenCache))
            return _tokenCache;

        if (TokenEnPreferencias)
        {
            var t = Preferences.Default.Get<string?>(TokenBackupKey, null);
            if (!string.IsNullOrEmpty(t))
            {
                _tokenCache = t;
                return _tokenCache;
            }
        }

        try
        {
            _tokenCache = await Task.Run(() => SecureStorage.Default.GetAsync(TokenKey));
        }
        catch
        {
            _tokenCache = null;
        }
        return _tokenCache;
    }

    /// Precarga el token al arrancar la app.
    public async Task InitAsync()
    {
        // Preferences primero (sync).
        if (TokenEnPreferencias)
        {
            var t = Preferences.Default.Get<string?>(TokenBackupKey, null);
            if (!string.IsNullOrEmpty(t))
            {
                _tokenCache = t;
                return;
            }
        }

        // SecureStorage como fallback.
        try
        {
            _tokenCache = await Task.Run(() => SecureStorage.Default.GetAsync(TokenKey));
        }
        catch
        {
            _tokenCache = null;
        }
    }

    public bool     HasToken     => !string.IsNullOrEmpty(_tokenCache);
    public string?  CachedToken  => _tokenCache;
    public bool     ProbablyLoggedIn => Preferences.Default.Get(LoggedInKey, false);

    public async Task StoreSessionAsync(AuthResponse response)
    {
        // Todos los estados sincrónicos primero — disponibles al instante para
        // cualquier request que se dispare tras la navegación a Home.
        _tokenCache      = response.Token;
        _ultimoLoginUtc  = DateTime.UtcNow;

        Preferences.Default.Set(NombreKey,    response.NombreCompleto);
        Preferences.Default.Set(DniKey,       response.Dni);
        Preferences.Default.Set(TelefonoKey,  response.Telefono);
        Preferences.Default.Set(IdKey,        response.IdVictima);
        Preferences.Default.Set(LoggedInKey,  true);
        if (TokenEnPreferencias)
            Preferences.Default.Set(TokenBackupKey, response.Token);

        // SecureStorage async al final (más lento, pero más seguro).
        try
        {
            await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        }
        catch
        {
            // Si Keystore falla, el token sigue disponible en cache + Preferences.
        }
    }

    public void ClearSession()
    {
        _tokenCache  = null;
        _perfilCache = null;
        ContactosCacheInvalidado = true;

        Preferences.Default.Remove(NombreKey);
        Preferences.Default.Remove(DniKey);
        Preferences.Default.Remove(TelefonoKey);
        Preferences.Default.Remove(IdKey);
        Preferences.Default.Remove(LoggedInKey);
        Preferences.Default.Remove(TokenBackupKey);

        Task.Run(() => { try { SecureStorage.Default.Remove(TokenKey); } catch { } });
    }

    public string GetInitials()
    {
        var nombre = NombreCompleto ?? "?";
        var parts  = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : nombre.Length > 0 ? nombre[..1].ToUpper() : "?";
    }
}
