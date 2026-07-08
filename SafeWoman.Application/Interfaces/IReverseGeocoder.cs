namespace SafeWoman.Application.Interfaces;

/// Servicio de reverse-geocoding: traduce coordenadas a un texto de dirección
/// legible (calle, distrito, ciudad). Usado en el SMS SOS para que el contacto
/// vea directamente dónde ocurre la emergencia sin depender solo del link.
public interface IReverseGeocoder
{
    /// Devuelve una dirección legible corta o null si el servicio falla.
    Task<string?> LookupAsync(decimal lat, decimal lng, CancellationToken ct = default);
}
