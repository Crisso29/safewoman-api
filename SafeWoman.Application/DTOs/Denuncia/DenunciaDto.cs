using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.Denuncia;

public record DenunciaDto(
    int IdDenuncia,
    TipoDenuncia Tipo,
    EstadoDenuncia Estado,
    DateTime FechaEnvio,
    string? Departamento,
    string? Provincia,
    string? Distrito,
    string? Descripcion,
    string? NombreAliasDenunciado,
    string? RelacionDenunciado,
    IReadOnlyList<EvidenciaDto> Evidencias
);

public record EvidenciaDto(
    int IdEvidencia,
    string NombreArchivo,
    TipoArchivo TipoArchivo,
    long? TamanioBytes,
    DateTime FechaSubida
);
