namespace SafeWoman.Application.DTOs.Auth;

public record RegistroRequest(
    string NombreCompleto,
    string Dni,
    string Telefono,
    string Password
);
