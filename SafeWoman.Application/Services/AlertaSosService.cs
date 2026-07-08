using Microsoft.Extensions.Logging;
using SafeWoman.Application.DTOs.AlertaSos;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class AlertaSosService
{
    private readonly IRepository<AlertaSos>          _alertaRepo;
    private readonly IRepository<Victima>            _victimaRepo;
    private readonly IRepository<ContactoEmergencia> _contactoRepo;
    private readonly IUnitOfWork                     _uow;
    private readonly ISosSmsNotifier                 _sosSms;
    private readonly ISosNotifier                    _notifier;
    private readonly IReverseGeocoder                _reverseGeocoder;
    private readonly ILogger<AlertaSosService>       _logger;

    public AlertaSosService(
        IRepository<AlertaSos> alertaRepo,
        IRepository<Victima> victimaRepo,
        IRepository<ContactoEmergencia> contactoRepo,
        IUnitOfWork uow,
        ISosSmsNotifier sosSms,
        ISosNotifier notifier,
        IReverseGeocoder reverseGeocoder,
        ILogger<AlertaSosService> logger)
    {
        _alertaRepo       = alertaRepo;
        _victimaRepo      = victimaRepo;
        _contactoRepo     = contactoRepo;
        _uow              = uow;
        _sosSms           = sosSms;
        _notifier         = notifier;
        _reverseGeocoder  = reverseGeocoder;
        _logger           = logger;
    }

    public async Task<AlertaSosDto> ActivarAsync(int idVictima, ActivarSosRequest req, CancellationToken ct = default)
    {
        var victima = await _victimaRepo.GetByIdAsync(idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");

        // Verificar contactos ANTES de persistir la alerta para no dejar registros huérfanos.
        var contactos = await _contactoRepo.FindAsync(c => c.IdVictima == idVictima, ct);

        if (!contactos.Any())
            throw new DomainException(
                "Necesita al menos un contacto de emergencia para activar el SOS. " +
                "Agréguelo desde la sección Contactos de su perfil.");

        var alerta = AlertaSos.Activar(idVictima, req.Latitud, req.Longitud);
        await _alertaRepo.AddAsync(alerta, ct);
        await _uow.SaveChangesAsync(ct);

        // Reverse-geocoding UNA vez para incluir la dirección textual en TODOS los SMS.
        // Así el contacto sabe dónde ir aunque no abra el link, y el texto coincide
        // exactamente con lo que el admin ve en el panel (misma fuente OSM/Nominatim).
        // Es best-effort: si Nominatim tarda o falla, el SMS igual sale con las coords.
        var direccion = await _reverseGeocoder.LookupAsync(req.Latitud, req.Longitud, ct);

        // SMS no-critico: un fallo de Twilio no debe bloquear el SOS ya registrado.
        var smsTasks = contactos.Select(async c =>
        {
            try { await _sosSms.SendSosAlertAsync(c.Telefono, victima.NombreCompleto, req.Latitud, req.Longitud, alerta.TimestampActivacion, direccion, ct); }
            catch (Exception ex) { _logger.LogError(ex, "SMS SOS fallido al contacto {Tel}", c.Telefono); }
        });

        var notifyTask = Task.Run(async () =>
        {
            try { await _notifier.NotifyNewAlertAsync(alerta.IdAlerta, victima.NombreCompleto, victima.Telefono, req.Latitud, req.Longitud, alerta.TimestampActivacion, ct); }
            catch (Exception ex) { _logger.LogError(ex, "SignalR SOS fallido para alerta {Id}", alerta.IdAlerta); }
        }, ct);

        await Task.WhenAll(smsTasks.Append(notifyTask));

        return MapToDto(alerta, victima);
    }

    public async Task<AlertaSosDto> CancelarAsync(int idVictima, int idAlerta, CancellationToken ct = default)
    {
        var alerta = await _alertaRepo.GetByIdAsync(idAlerta, ct)
            ?? throw new DomainException("Alerta no encontrada.");

        if (alerta.IdVictima != idVictima)
            throw new DomainException("No autorizado.");

        alerta.Cancelar();
        await _uow.SaveChangesAsync(ct);

        var victima = await _victimaRepo.GetByIdAsync(idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");

        var contactos = await _contactoRepo.FindAsync(c => c.IdVictima == idVictima, ct);

        var smsTasks = contactos.Select(async c =>
        {
            try { await _sosSms.SendCancelacionSosAsync(c.Telefono, victima.NombreCompleto, ct); }
            catch (Exception ex) { _logger.LogError(ex, "SMS cancelación fallido al contacto {Tel}", c.Telefono); }
        });

        var notifyCancel = Task.Run(async () =>
        {
            try { await _notifier.NotifyAlertCancelledAsync(idAlerta, ct); }
            catch (Exception ex) { _logger.LogError(ex, "SignalR cancelación fallido para alerta {Id}", idAlerta); }
        }, ct);

        await Task.WhenAll(smsTasks.Append(notifyCancel));

        return MapToDto(alerta, victima);
    }

    public async Task<IReadOnlyList<AlertaSosDto>> ListarPorVictimaAsync(int idVictima, CancellationToken ct = default)
    {
        var victima = await _victimaRepo.GetByIdAsync(idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");

        var alertas = await _alertaRepo.FindAsync(a => a.IdVictima == idVictima, ct);
        return alertas.OrderByDescending(a => a.TimestampActivacion)
                      .Select(a => MapToDto(a, victima))
                      .ToList();
    }

    private static AlertaSosDto MapToDto(AlertaSos a, Victima v) =>
        new(a.IdAlerta, a.IdVictima, v.NombreCompleto, v.Telefono,
            a.Latitud, a.Longitud, a.TimestampActivacion, a.TimestampCancelacion, a.Estado);
}
