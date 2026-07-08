namespace SafeWoman.Application.DTOs.Auth;

// Identificador puede ser el número de teléfono (9 dígitos) o el DNI (8 dígitos) — RF-04
public record LoginRequest(
    string Identificador,
    string Password
);
