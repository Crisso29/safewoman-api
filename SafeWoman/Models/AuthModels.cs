namespace SafeWoman.Models;

public record RegistroRequest(string NombreCompleto, string Dni, string Telefono, string Password);
// RF-04: Identificador puede ser teléfono (9 dígitos) o DNI (8 dígitos)
public record LoginRequest(string Identificador, string Password);
public record VerificarOtpRequest(string Telefono, string Codigo);

public class AuthResponse
{
    public string Token          { get; set; } = default!;
    public int    IdVictima      { get; set; }
    public string NombreCompleto { get; set; } = default!;
    public string Dni            { get; set; } = default!;
    public string Telefono       { get; set; } = default!;
    public bool   Verificada     { get; set; }
}
