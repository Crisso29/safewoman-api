using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class EvidenciaAnonima
{
    public int IdEvidenciaAn { get; private set; }
    public int IdDenunciaAnonima { get; private set; }
    public string NombreArchivo { get; private set; } = default!;
    public string RutaArchivo { get; private set; } = default!;
    public TipoArchivo TipoArchivo { get; private set; }
    public long? TamanioBytes { get; private set; }
    public DateTime FechaSubida { get; private set; }

    public DenunciaAnonima DenunciaAnonima { get; private set; } = default!;

    private EvidenciaAnonima() { }

    public static EvidenciaAnonima Crear(int idDenunciaAnonima, string nombreArchivo,
        string rutaArchivo, TipoArchivo tipo, long? tamanioBytes)
    {
        return new EvidenciaAnonima
        {
            IdDenunciaAnonima = idDenunciaAnonima,
            NombreArchivo = nombreArchivo,
            RutaArchivo = rutaArchivo,
            TipoArchivo = tipo,
            TamanioBytes = tamanioBytes,
            FechaSubida = DateTime.UtcNow
        };
    }
}
