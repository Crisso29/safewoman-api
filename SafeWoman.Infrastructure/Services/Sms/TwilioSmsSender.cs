using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SafeWoman.Application.Interfaces;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;

namespace SafeWoman.Infrastructure.Services.Sms;

public class TwilioSmsSender : IOtpSender, ISosSmsNotifier
{
    private readonly string _fromNumber;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(IConfiguration config, ILogger<TwilioSmsSender> logger)
    {
        _logger = logger;
        _fromNumber = config["Twilio:FromNumber"]!;
        TwilioClient.Init(config["Twilio:AccountSid"]!, config["Twilio:AuthToken"]!);
    }

    public Task SendOtpAsync(string toPhone, string code, CancellationToken ct = default)
    {
        var body = $"SafeWoman: Tu código de verificación es {code}. Válido por 5 minutos.";
        return SendAsync(FormatPeruPhone(toPhone), body);
    }

    // Límite de un segmento SMS en GSM-7 (encoding estándar sin emojis).
    // Superarlo divide el mensaje en varios segmentos y Twilio cobra por cada uno.
    private const int MaxCharsUnSegmento = 160;

    public Task SendSosAlertAsync(string toPhone, string victimName, decimal lat, decimal lng,
        DateTime timestamp, string? direccion, CancellationToken ct = default)
    {
        // Coordenadas con 5 decimales — precisión de ~1 metro, suficiente para SOS.
        // 7 decimales daban precisión submétrica innecesaria y URL más larga.
        var latStr = lat.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
        var lngStr = lng.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);

        // URL de Google Maps corta con zoom nivel calle (~z=17) que fuerza pin exacto.
        // Ejemplo: https://maps.google.com/?q=-13.16017,-74.22012&z=17  (50 chars)
        var mapsLink = $"https://maps.google.com/?q={latStr},{lngStr}&z=17";
        var hora     = timestamp.ToLocalTime().ToString("HH:mm");

        // Base del mensaje SIN dirección — siempre cabe en 1 segmento (GSM-7).
        // Sin emojis, con tildes suaves permitidas por GSM-7 (á, é, í, ó, ú, ñ).
        var baseMsg = $"SafeWoman SOS: {victimName} - alerta {hora}. {mapsLink}";

        // Si queda espacio en el segmento, adjuntamos la dirección (truncada si hace falta).
        string body;
        var espacioLibre = MaxCharsUnSegmento - baseMsg.Length - 4; // 4 chars margen " (…)"
        if (!string.IsNullOrWhiteSpace(direccion) && espacioLibre > 15)
        {
            var dirCorta = direccion.Length > espacioLibre
                ? direccion[..espacioLibre].TrimEnd(',', ' ') + "…"
                : direccion;
            body = $"{baseMsg} ({dirCorta})";
        }
        else
        {
            body = baseMsg;
        }

        return SendAsync(FormatPeruPhone(toPhone), body);
    }

    public Task SendCancelacionSosAsync(string toPhone, string victimName, CancellationToken ct = default)
    {
        // Corto y sin emojis para caber siempre en 1 segmento.
        var body = $"SafeWoman: {victimName} canceló la alerta SOS. Ya está a salvo.";
        return SendAsync(FormatPeruPhone(toPhone), body);
    }

    private async Task SendAsync(string to, string body)
    {
        try
        {
            var message = await MessageResource.CreateAsync(
                to:   new Twilio.Types.PhoneNumber(to),
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                body: body);

            _logger.LogInformation("SMS enviado a {To} — SID: {Sid} Estado: {Status}",
                to, message.Sid, message.Status);
        }
        catch (ApiException apiEx)
        {
            // Error de Twilio con código específico
            _logger.LogError(
                "Twilio error {Code} enviando a {To}: {Message}. " +
                "Causa probable: cuenta trial (solo números verificados), " +
                "número inválido o saldo insuficiente. " +
                "Verifica en https://console.twilio.com",
                apiEx.Code, to, apiEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando SMS a {To}", to);
            throw;
        }
    }

    private static string FormatPeruPhone(string phone) =>
        phone.StartsWith("+") ? phone : $"+51{phone}";
}
