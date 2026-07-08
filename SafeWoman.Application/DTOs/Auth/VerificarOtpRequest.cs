namespace SafeWoman.Application.DTOs.Auth;

public record VerificarOtpRequest(
    string Telefono,
    string Codigo
);
