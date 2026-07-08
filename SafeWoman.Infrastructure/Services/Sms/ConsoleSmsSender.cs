using Microsoft.Extensions.Logging;
using SafeWoman.Application.Interfaces;

namespace SafeWoman.Infrastructure.Services.Sms;

/// Implementación de OTP + SOS SMS que NO envía nada real.
/// Escribe los mensajes en la consola/logger para desarrollo — cero costo Twilio.
///
/// Ideal para probar registro, login-OTP y SOS sin gastar el saldo de la cuenta trial.
/// El OTP aparece con formato destacado en la consola de la API:
///
///     ╔════════════════════════════════════════════════╗
///     ║  [SMS FAKE]  OTP → +51987654321                ║
///     ║  Código: 123456   (válido 5 min)               ║
///     ╚════════════════════════════════════════════════╝
///
/// Se activa cuando appsettings.json tiene "Sms:Provider": "Console"
/// (o cuando la clave está ausente en Development por seguridad de saldo).
public class ConsoleSmsSender : IOtpSender, ISosSmsNotifier
{
    private readonly ILogger<ConsoleSmsSender> _logger;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendOtpAsync(string toPhone, string code, CancellationToken ct = default)
    {
        var to = FormatPeruPhone(toPhone);
        var msg = $"""

            ╔════════════════════════════════════════════════╗
            ║  [SMS FAKE]  OTP → {to,-20}         ║
            ║  Código: {code,-8}     (válido 5 min)          ║
            ╚════════════════════════════════════════════════╝
            """;
        Console.WriteLine(msg);
        _logger.LogInformation("Console SMS OTP a {To}: {Code}", to, code);
        return Task.CompletedTask;
    }

    public Task SendSosAlertAsync(string toPhone, string victimName, decimal lat, decimal lng,
        DateTime timestamp, string? direccion, CancellationToken ct = default)
    {
        var to = FormatPeruPhone(toPhone);
        var msg = $"""

            ╔════════════════════════════════════════════════╗
            ║  [SMS FAKE]  ALERTA SOS → {to,-20}
            ║  Víctima  : {victimName}
            ║  Hora     : {timestamp.ToLocalTime():HH:mm}
            ║  Ubicación: {lat:F6}, {lng:F6}
            ║  Dirección: {direccion ?? "(no resuelta)"}
            ╚════════════════════════════════════════════════╝
            """;
        Console.WriteLine(msg);
        _logger.LogInformation(
            "Console SMS SOS a {To} — víctima {Victim} @ ({Lat},{Lng})",
            to, victimName, lat, lng);
        return Task.CompletedTask;
    }

    public Task SendCancelacionSosAsync(string toPhone, string victimName, CancellationToken ct = default)
    {
        var to = FormatPeruPhone(toPhone);
        Console.WriteLine($"[SMS FAKE]  CANCELACIÓN SOS → {to}   |   {victimName} está a salvo.");
        _logger.LogInformation("Console SMS cancelación a {To} — {Victim}", to, victimName);
        return Task.CompletedTask;
    }

    private static string FormatPeruPhone(string phone) =>
        phone.StartsWith("+") ? phone : $"+51{phone}";
}
