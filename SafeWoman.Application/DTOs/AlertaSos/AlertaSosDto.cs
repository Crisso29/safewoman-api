using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.AlertaSos;

public record AlertaSosDto(
    int IdAlerta,
    int IdVictima,
    string NombreVictima,
    string TelefonoVictima,
    decimal Latitud,
    decimal Longitud,
    DateTime TimestampActivacion,
    DateTime? TimestampCancelacion,
    EstadoAlerta Estado
);
