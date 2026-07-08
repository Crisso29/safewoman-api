using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.DenunciaAnonima;

public record DenunciaAnonimaDto(
    int IdDenunciaAnonima,
    EstadoDenuncia Estado,
    DateTime FechaEnvio,
    string? Departamento,
    string? Provincia,
    string? Distrito,
    string? Descripcion,
    string? NombreAliasDenunciado,
    string? RelacionDenunciado
);
