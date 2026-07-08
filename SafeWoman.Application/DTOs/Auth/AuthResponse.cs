namespace SafeWoman.Application.DTOs.Auth;

public record AuthResponse(
    string Token,
    int IdVictima,
    string NombreCompleto,
    string Dni,
    string Telefono,
    bool Verificada
);
