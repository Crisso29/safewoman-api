using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.DenunciaAnonima;

public record DenunciaAnonimaRequest(
    string DeviceFingerprint,
    string? NombreAliasDenunciado,
    RelacionVictima? RelacionDenunciado,
    string? Departamento,
    string? Provincia,
    string? Distrito,
    string? ReferenciaUbicacion,
    decimal? Latitud,
    decimal? Longitud,
    DateOnly? FechaHecho,
    TimeOnly? HoraHecho,
    string? Descripcion
);

/// <summary>
/// Vista resumida de una denuncia anónima para el seguimiento en la app.
/// NO incluye datos sensibles (denunciado, ubicación exacta) para reducir
/// riesgo si el teléfono es comprometido — solo estado, fecha y descripción corta.
/// </summary>
public record DenunciaAnonimaResumenDto(
    int IdDenunciaAnonima,
    EstadoDenuncia Estado,
    DateTime FechaEnvio,
    string? Descripcion
);
