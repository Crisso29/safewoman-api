using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.Denuncia;

public record DenunciaFormalRequest(
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
    string Descripcion
);
