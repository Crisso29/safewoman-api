using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class DenunciadoAnonima
{
    public int IdDenunciadoAn { get; private set; }
    public int IdDenunciaAnonima { get; private set; }
    public string? NombreAlias { get; private set; }
    public RelacionVictima? Relacion { get; private set; }

    public DenunciaAnonima DenunciaAnonima { get; private set; } = default!;

    private DenunciadoAnonima() { }

    public static DenunciadoAnonima Crear(int idDenunciaAnonima, string? nombreAlias, RelacionVictima? relacion)
    {
        return new DenunciadoAnonima
        {
            IdDenunciaAnonima = idDenunciaAnonima,
            NombreAlias = nombreAlias?.Trim(),
            Relacion = relacion
        };
    }
}
