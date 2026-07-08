using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SafeWoman.Models;

namespace SafeWoman.Services;

/// Cliente de la API.
///
/// Estrategia de autenticación (defensa en profundidad):
///   Cada request que sale de aquí construye su HttpRequestMessage e inyecta el
///   Bearer manualmente. NO dependemos del DelegatingHandler porque en algunos
///   escenarios de MAUI/HttpClientFactory el pipeline puede resolver una instancia
///   de AuthStateService con cache sin el token que acabamos de guardar.
///   Leer directo desde Preferences (singleton global de MAUI Essentials) elimina
///   ese drama por completo.
public class ApiService
{
    private const string TokenBackupKey = "jwt_token_backup";

    private readonly HttpClient _http;
    private readonly AuthStateService _authState;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiService(HttpClient http, AuthStateService authState)
    {
        _http      = http;
        _authState = authState;
    }

    // ═══ AUTH ═══════════════════════════════════════════════════════════════════
    public Task<ApiResponse<AuthResponse>> RegistrarAsync(RegistroRequest req) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "auth/registro", req);

    public Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "auth/login", req);

    public Task<ApiResponse<AuthResponse>> VerificarOtpAsync(VerificarOtpRequest req) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "auth/verificar-otp", req);

    public Task<ApiResponse<object>> ReenviarOtpAsync(string telefono) =>
        SendAsync<object>(HttpMethod.Post, "auth/reenviar-otp", new { telefono });

    // ═══ PERFIL / SOS / CONTACTOS ═══════════════════════════════════════════════
    public Task<ApiResponse<VictimaPerfilDto>> ObtenerPerfilAsync() =>
        SendAsync<VictimaPerfilDto>(HttpMethod.Get, "victima/perfil");

    public Task<ApiResponse<AlertaSosDto>> ActivarSosAsync(ActivarSosRequest req) =>
        SendAsync<AlertaSosDto>(HttpMethod.Post, "sos/activar", req);

    public Task<ApiResponse<AlertaSosDto>> CancelarSosAsync(int idAlerta) =>
        SendAsync<AlertaSosDto>(HttpMethod.Post, $"sos/{idAlerta}/cancelar");

    public Task<ApiResponse<IReadOnlyList<ContactoEmergenciaDto>>> ListarContactosAsync() =>
        SendAsync<IReadOnlyList<ContactoEmergenciaDto>>(HttpMethod.Get, "contactos");

    public Task<ApiResponse<ContactoEmergenciaDto>> CrearContactoAsync(string nombre, string telefono) =>
        SendAsync<ContactoEmergenciaDto>(HttpMethod.Post, "contactos", new { nombre, telefono });

    public Task<ApiResponse<bool>> EliminarContactoAsync(int idContacto) =>
        SendVoidAsync(HttpMethod.Delete, $"contactos/{idContacto}");

    // ═══ DENUNCIAS (multipart) ══════════════════════════════════════════════════
    public Task<ApiResponse<object>> EnviarDenunciaFormalAsync(MultipartFormDataContent form) =>
        SendMultipartAsync<object>("denuncias/formal", form);

    public Task<ApiResponse<object>> EnviarDenunciaAnonimaAsync(MultipartFormDataContent form) =>
        SendMultipartAsync<object>("denuncias/anonima", form);

    // ═══ DENUNCIAS — seguimiento (mis denuncias) ═══════════════════════════════

    /// <summary>Mis denuncias formales (requiere JWT — el server usa el idVictima del token).</summary>
    public Task<ApiResponse<IReadOnlyList<DenunciaDto>>> ObtenerMisDenunciasAsync() =>
        SendAsync<IReadOnlyList<DenunciaDto>>(HttpMethod.Get, "denuncias");

    /// <summary>Mis denuncias anónimas (sin JWT — se filtran por device fingerprint).</summary>
    public Task<ApiResponse<IReadOnlyList<DenunciaAnonimaResumenDto>>> ObtenerMisDenunciasAnonimasAsync(
        string deviceFingerprint) =>
        SendAsync<IReadOnlyList<DenunciaAnonimaResumenDto>>(
            HttpMethod.Get,
            $"denuncias/anonima/mis?deviceFingerprint={Uri.EscapeDataString(deviceFingerprint)}");

    // ═══ CORE ═══════════════════════════════════════════════════════════════════

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, HttpContent? body = null)
    {
        var req = new HttpRequestMessage(method, path);
        if (body is not null) req.Content = body;

        // Inyectamos el Bearer DIRECTO — leemos el token con doble fallback:
        // primero el cache del servicio, luego Preferences (siempre disponible).
        var token = _authState.CachedToken;
        if (string.IsNullOrEmpty(token))
            token = Preferences.Default.Get<string?>(TokenBackupKey, null);

        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[Api] {method} {path}  →  " +
            (string.IsNullOrEmpty(token) ? "SIN TOKEN" : $"Bearer {token[..Math.Min(20, token.Length)]}…"));
#endif
        return req;
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        HttpContent? content = null;
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        try
        {
            using var req      = BuildRequest(method, path, content);
            using var response = await _http.SendAsync(req);
            return await ParseAsync<T>(response);
        }
        catch (Exception ex) { return HandleException<T>(ex); }
    }

    private async Task<ApiResponse<bool>> SendVoidAsync(HttpMethod method, string path)
    {
        try
        {
            using var req      = BuildRequest(method, path);
            using var response = await _http.SendAsync(req);
            if (response.IsSuccessStatusCode)
                return new ApiResponse<bool>(true, true, null);
            var parsed = await ParseAsync<object>(response);
            return new ApiResponse<bool>(false, false, parsed.Error);
        }
        catch (Exception ex)
        {
            var mapped = HandleException<object>(ex);
            return new ApiResponse<bool>(false, false, mapped.Error);
        }
    }

    private async Task<ApiResponse<T>> SendMultipartAsync<T>(string path, MultipartFormDataContent form)
    {
        try
        {
            using var req      = BuildRequest(HttpMethod.Post, path, form);
            using var response = await _http.SendAsync(req);
            return await ParseAsync<T>(response);
        }
        catch (Exception ex) { return HandleException<T>(ex); }
    }

    private static ApiResponse<T> HandleException<T>(Exception ex) => ex switch
    {
        TaskCanceledException =>
            new ApiResponse<T>(false, default, "No se pudo conectar con el servidor. Verifique que la API esté ejecutándose."),
        OperationCanceledException =>
            new ApiResponse<T>(false, default, "La solicitud fue cancelada. Intente nuevamente."),
        HttpRequestException =>
            new ApiResponse<T>(false, default, "Sin conexión al servidor. Verifique su red o que la API esté activa."),
        JsonException je =>
            new ApiResponse<T>(false, default, $"Respuesta inesperada del servidor: {je.Message}"),
        _ =>
            new ApiResponse<T>(false, default, $"Error inesperado: {ex.GetType().Name} — {ex.Message}")
    };

    private static async Task<ApiResponse<T>> ParseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(content, JsonOpts);
            return new ApiResponse<T>(true, data, null);
        }

        // Mensajes específicos para códigos comunes — más útiles que "Error 401".
        var mensajePorStatus = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Sesión expirada. Cierre sesión y vuelva a iniciarla.",
            System.Net.HttpStatusCode.Forbidden    => "No tiene permiso para realizar esta acción.",
            System.Net.HttpStatusCode.NotFound     => "El recurso solicitado no existe.",
            System.Net.HttpStatusCode.TooManyRequests
                => "Demasiadas solicitudes. Espere un momento y vuelva a intentar.",
            System.Net.HttpStatusCode.ServiceUnavailable
                => "El servidor no está disponible en este momento.",
            _ => null
        };

        string? error = null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var ep) && ep.ValueKind == JsonValueKind.String)
                error = ep.GetString();
            else if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var msgs = errs.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(v => v.GetString()).OfType<string>()
                        : Enumerable.Empty<string>())
                    .ToList();
                error = msgs.Count > 0
                    ? string.Join(". ", msgs)
                    : (root.TryGetProperty("title", out var t) ? t.GetString() : null);
            }
            else if (root.TryGetProperty("detail", out var detailProp))
                error = detailProp.GetString();
            else if (root.TryGetProperty("title", out var titleProp))
                error = titleProp.GetString();
        }
        catch { /* JSON malformado o body vacío */ }

        error ??= mensajePorStatus ?? $"Error del servidor ({(int)response.StatusCode})";

        return new ApiResponse<T>(false, default, error);
    }
}
