namespace SafeWoman.Application.Interfaces;

public interface ISosSmsNotifier
{
    /// Envía SMS de alerta SOS a un contacto de emergencia.
    /// <param name="direccion">Dirección textual del lugar (reverse-geocoded).
    /// Puede ser null si el geocoder falló — el SMS igual saldrá con las coordenadas.</param>
    Task SendSosAlertAsync(string toPhone, string victimName, decimal lat, decimal lng,
        DateTime timestamp, string? direccion, CancellationToken ct = default);

    Task SendCancelacionSosAsync(string toPhone, string victimName, CancellationToken ct = default);
}
