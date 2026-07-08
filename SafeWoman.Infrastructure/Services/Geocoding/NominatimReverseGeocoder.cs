using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeWoman.Application.Interfaces;

namespace SafeWoman.Infrastructure.Services.Geocoding;

/// Reverse-geocoder que usa Nominatim (OpenStreetMap) — gratuito, sin key.
/// Se usa para incluir en el SMS SOS la dirección textual del punto,
/// para que el contacto sepa dónde está la víctima sin depender solo del link.
public class NominatimReverseGeocoder : IReverseGeocoder
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://nominatim.openstreetmap.org/"),
        Timeout     = TimeSpan.FromSeconds(5)
    };

    private readonly ILogger<NominatimReverseGeocoder> _logger;

    static NominatimReverseGeocoder()
    {
        // Nominatim exige User-Agent identificado
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "SafeWoman/1.0 (UNSCH; crisologo.aguilar.27@unsch.edu.pe)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public NominatimReverseGeocoder(ILogger<NominatimReverseGeocoder> logger)
    {
        _logger = logger;
    }

    public async Task<string?> LookupAsync(decimal lat, decimal lng, CancellationToken ct = default)
    {
        try
        {
            var latS = lat.ToString("F6", CultureInfo.InvariantCulture);
            var lngS = lng.ToString("F6", CultureInfo.InvariantCulture);
            var url  = $"reverse?lat={latS}&lon={lngS}&format=json&accept-language=es&zoom=18";

            var json = await _http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("display_name", out var dn))
                return null;

            var full = dn.GetString();
            if (string.IsNullOrWhiteSpace(full)) return null;

            // display_name suele ser muy largo. Nos quedamos con las primeras 3-4
            // porciones (calle, barrio, distrito) para que el SMS quepa en 160 chars.
            var partes = full.Split(',', StringSplitOptions.TrimEntries);
            var cortas = partes.Take(4);
            return string.Join(", ", cortas);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse-geocoding fallido para ({Lat},{Lng})", lat, lng);
            return null;
        }
    }
}
