namespace SafeWoman.Domain.Exceptions;

/// Códigos estables de error usados en las respuestas HTTP y consumidos
/// por el frontend móvil para reaccionar por código (redirigir al login,
/// mostrar diálogo específico, etc.) en vez de hacer string matching.
/// NUNCA cambiar el valor de un código ya existente — es contrato con la app.
public static class ErrorCodes
{
    // ── Registro ─────────────────────────────────────────────────────────────
    public const string ACCOUNT_ALREADY_VERIFIED = "ACCOUNT_ALREADY_VERIFIED";

    // ── Login ────────────────────────────────────────────────────────────────
    public const string INVALID_CREDENTIALS      = "INVALID_CREDENTIALS";
    public const string ACCOUNT_NOT_VERIFIED     = "ACCOUNT_NOT_VERIFIED";

    // ── OTP ──────────────────────────────────────────────────────────────────
    public const string PHONE_NOT_FOUND          = "PHONE_NOT_FOUND";
    public const string OTP_NOT_FOUND            = "OTP_NOT_FOUND";
    public const string OTP_INVALID              = "OTP_INVALID";
    public const string OTP_ALREADY_USED         = "OTP_ALREADY_USED";
}
