using Microsoft.AspNetCore.SignalR;
using SafeWoman.Application.Interfaces;

namespace SafeWoman.Infrastructure.Services.Realtime;

public class SignalRSosNotifier : ISosNotifier
{
    private readonly IHubContext<SosHub> _hub;

    public SignalRSosNotifier(IHubContext<SosHub> hub) => _hub = hub;

    public Task NotifyNewAlertAsync(int idAlerta, string nombreVictima, string telefono,
        decimal lat, decimal lng, DateTime timestamp, CancellationToken ct = default)
    {
        return _hub.Clients.All.SendAsync("NuevaAlertaSos", new
        {
            idAlerta,
            nombreVictima,
            telefono,
            lat,
            lng,
            timestamp = timestamp.ToString("O")
        }, ct);
    }

    public Task NotifyAlertCancelledAsync(int idAlerta, CancellationToken ct = default)
    {
        return _hub.Clients.All.SendAsync("AlertaCancelada", new { idAlerta }, ct);
    }
}
