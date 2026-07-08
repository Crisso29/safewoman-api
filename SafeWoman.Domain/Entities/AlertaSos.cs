using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.Domain.Entities;

public class AlertaSos
{
    public int IdAlerta { get; private set; }
    public int IdVictima { get; private set; }
    public decimal Latitud { get; private set; }
    public decimal Longitud { get; private set; }
    public DateTime TimestampActivacion { get; private set; }
    public DateTime? TimestampCancelacion { get; private set; }
    public EstadoAlerta Estado { get; private set; }

    public Victima Victima { get; private set; } = default!;

    private AlertaSos() { }

    public static AlertaSos Activar(int idVictima, decimal latitud, decimal longitud)
    {
        return new AlertaSos
        {
            IdVictima = idVictima,
            Latitud = latitud,
            Longitud = longitud,
            TimestampActivacion = DateTime.UtcNow,
            Estado = EstadoAlerta.Activa
        };
    }

    public void Cancelar()
    {
        if (Estado != EstadoAlerta.Activa)
            throw new DomainException("Solo se puede cancelar una alerta activa.");

        Estado = EstadoAlerta.Cancelada;
        TimestampCancelacion = DateTime.UtcNow;
    }

    public void Atender()
    {
        if (Estado != EstadoAlerta.Activa)
            throw new DomainException("Solo se puede atender una alerta activa.");

        Estado = EstadoAlerta.Atendida;
        TimestampCancelacion = DateTime.UtcNow;
    }

}
