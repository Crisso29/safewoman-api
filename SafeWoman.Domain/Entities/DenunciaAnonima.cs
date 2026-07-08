using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class DenunciaAnonima
{
    public int IdDenunciaAnonima { get; private set; }
    public int IdHuella { get; private set; }
    public EstadoDenuncia Estado { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public string? Departamento { get; private set; }
    public string? Provincia { get; private set; }
    public string? Distrito { get; private set; }
    public string? ReferenciaUbicacion { get; private set; }
    public decimal? LatHecho { get; private set; }
    public decimal? LngHecho { get; private set; }
    public DateOnly? FechaHecho { get; private set; }
    public TimeOnly? HoraHecho { get; private set; }
    public string? Descripcion { get; private set; }

    public HuellaDispositivo HuellaDispositivo { get; private set; } = default!;
    public DenunciadoAnonima? Denunciado { get; private set; }
    public IReadOnlyCollection<EvidenciaAnonima> Evidencias => _evidencias.AsReadOnly();

    private readonly List<EvidenciaAnonima> _evidencias = [];

    private DenunciaAnonima() { }

    public static DenunciaAnonima Crear(
        int idHuella,
        string? departamento, string? provincia, string? distrito,
        string? referenciaUbicacion, decimal? lat, decimal? lng,
        DateOnly? fechaHecho, TimeOnly? horaHecho,
        string? descripcion)
    {
        return new DenunciaAnonima
        {
            IdHuella = idHuella,
            Estado = EstadoDenuncia.Pendiente,
            FechaEnvio = DateTime.UtcNow,
            Departamento = departamento,
            Provincia = provincia,
            Distrito = distrito,
            ReferenciaUbicacion = referenciaUbicacion,
            LatHecho = lat,
            LngHecho = lng,
            FechaHecho = fechaHecho,
            HoraHecho = horaHecho,
            Descripcion = descripcion
        };
    }

    public void CambiarEstado(EstadoDenuncia nuevoEstado) => Estado = nuevoEstado;
}
