using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class Evidencia
{
    public int IdEvidencia { get; private set; }
    public int IdDenuncia { get; private set; }
    public string NombreArchivo { get; private set; } = default!;
    public string RutaArchivo { get; private set; } = default!;
    public TipoArchivo TipoArchivo { get; private set; }
    public long? TamanioBytes { get; private set; }
    public DateTime FechaSubida { get; private set; }

    public Denuncia Denuncia { get; private set; } = default!;

    private Evidencia() { }

    public static Evidencia Crear(int idDenuncia, string nombreArchivo, string rutaArchivo,
        TipoArchivo tipo, long? tamanioBytes)
    {
        return new Evidencia
        {
            IdDenuncia = idDenuncia,
            NombreArchivo = nombreArchivo,
            RutaArchivo = rutaArchivo,
            TipoArchivo = tipo,
            TamanioBytes = tamanioBytes,
            FechaSubida = DateTime.UtcNow
        };
    }
}
