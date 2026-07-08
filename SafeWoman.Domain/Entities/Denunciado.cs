using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class Denunciado
{
    public int IdDenunciado { get; private set; }
    public int IdDenuncia { get; private set; }
    public string? NombreAlias { get; private set; }
    public RelacionVictima? RelacionVictima { get; private set; }

    public Denuncia Denuncia { get; private set; } = default!;

    private Denunciado() { }

    public static Denunciado Crear(int idDenuncia, string? nombreAlias, RelacionVictima? relacion)
    {
        return new Denunciado
        {
            IdDenuncia = idDenuncia,
            NombreAlias = nombreAlias?.Trim(),
            RelacionVictima = relacion
        };
    }
}
