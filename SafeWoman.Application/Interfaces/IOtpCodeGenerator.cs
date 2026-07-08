namespace SafeWoman.Application.Interfaces;

/// <summary>
/// Genera códigos OTP criptográficamente seguros.
/// Separado de ITokenService (SRP): generar un código temporal
/// es una responsabilidad distinta a emitir un JWT.
/// </summary>
public interface IOtpCodeGenerator
{
    /// <summary>Devuelve un código numérico de 6 dígitos con padding cero.</summary>
    string Generate();
}
