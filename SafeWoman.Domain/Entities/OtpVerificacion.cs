using SafeWoman.Domain.Exceptions;

namespace SafeWoman.Domain.Entities;

public class OtpVerificacion
{
    public int IdOtp { get; private set; }
    public int IdVictima { get; private set; }
    public string Codigo { get; private set; } = default!;
    public DateTime FechaGeneracion { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public bool Usado { get; private set; }

    public Victima Victima { get; private set; } = default!;

    private OtpVerificacion() { }

    public static OtpVerificacion Crear(int idVictima, string codigo, int minutosValidez = 5)
    {
        var ahora = DateTime.UtcNow;
        return new OtpVerificacion
        {
            IdVictima = idVictima,
            Codigo = codigo,
            FechaGeneracion = ahora,
            FechaExpiracion = ahora.AddMinutes(minutosValidez),
            Usado = false
        };
    }

    public bool EsValido(string codigoIngresado)
    {
        return !Usado
            && DateTime.UtcNow <= FechaExpiracion
            && Codigo == codigoIngresado;
    }

    public void Consumir()
    {
        if (Usado) throw new DomainException("El código OTP ya fue utilizado.");
        Usado = true;
    }
}
