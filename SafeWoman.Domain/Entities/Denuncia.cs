using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class Denuncia
{
    public int           IdDenuncia         { get; private set; }
    public int           IdVictima          { get; private set; }
    public TipoDenuncia  Tipo               { get; private set; }
    public EstadoDenuncia Estado            { get; private set; }
    public DateTime      FechaEnvio         { get; private set; }
    public string?       FotoDniRuta        { get; private set; }
    public string?       Departamento       { get; private set; }
    public string?       Provincia          { get; private set; }
    public string?       Distrito           { get; private set; }
    public string?       ReferenciaUbicacion { get; private set; }
    public decimal?      LatHecho           { get; private set; }
    public decimal?      LngHecho           { get; private set; }
    public DateOnly?     FechaHecho         { get; private set; }
    public TimeOnly?     HoraHecho          { get; private set; }
    public string?       Descripcion        { get; private set; }
    public bool          DeclaracionJurada  { get; private set; }

    public Victima                         Victima    { get; private set; } = default!;
    public Denunciado?                     Denunciado { get; private set; }
    public IReadOnlyCollection<Evidencia>  Evidencias => _evidencias.AsReadOnly();

    private readonly List<Evidencia> _evidencias = [];

    private Denuncia() { }

    public static Denuncia CrearFormal(
        int idVictima,
        string fotoDniRuta,
        string? departamento, string? provincia, string? distrito,
        string? referenciaUbicacion, decimal? lat, decimal? lng,
        DateOnly? fechaHecho, TimeOnly? horaHecho,
        string descripcion)
    {
        return new Denuncia
        {
            IdVictima          = idVictima,
            Tipo               = TipoDenuncia.Formal,
            Estado             = EstadoDenuncia.Pendiente,
            FechaEnvio         = DateTime.UtcNow,
            FotoDniRuta        = fotoDniRuta,
            Departamento       = departamento,
            Provincia          = provincia,
            Distrito           = distrito,
            ReferenciaUbicacion = referenciaUbicacion,
            LatHecho           = lat,
            LngHecho           = lng,
            FechaHecho         = fechaHecho,
            HoraHecho          = horaHecho,
            Descripcion        = descripcion,
            DeclaracionJurada  = true
        };
    }

    public void CambiarEstado(EstadoDenuncia nuevoEstado)
    {
        if (Estado == nuevoEstado) return;
        Estado = nuevoEstado;
    }
}
