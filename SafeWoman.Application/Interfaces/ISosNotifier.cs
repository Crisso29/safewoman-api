namespace SafeWoman.Application.Interfaces;

public interface ISosNotifier
{
    Task NotifyNewAlertAsync(int idAlerta, string nombreVictima, string telefono,
        decimal lat, decimal lng, DateTime timestamp, CancellationToken ct = default);

    Task NotifyAlertCancelledAsync(int idAlerta, CancellationToken ct = default);
}
