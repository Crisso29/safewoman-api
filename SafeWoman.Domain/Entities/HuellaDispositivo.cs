using SafeWoman.Domain.Exceptions;

namespace SafeWoman.Domain.Entities;

public class HuellaDispositivo
{
    public int IdHuella { get; private set; }
    public string DeviceFingerprint { get; private set; } = default!;
    public bool Bloqueada { get; private set; }
    public DateTime FechaPrimerUso { get; private set; }
    public DateTime FechaUltimoUso { get; private set; }

    private HuellaDispositivo() { }

    public static HuellaDispositivo Crear(string fingerprint)
    {
        return new HuellaDispositivo
        {
            DeviceFingerprint = fingerprint,
            Bloqueada = false,
            FechaPrimerUso = DateTime.UtcNow,
            FechaUltimoUso = DateTime.UtcNow
        };
    }

    public void RegistrarUso() => FechaUltimoUso = DateTime.UtcNow;

    public void Bloquear()
    {
        if (Bloqueada) throw new DomainException("El dispositivo ya está bloqueado.");
        Bloqueada = true;
    }

    public void Desbloquear()
    {
        if (!Bloqueada) throw new DomainException("El dispositivo no está bloqueado.");
        Bloqueada = false;
    }
}
