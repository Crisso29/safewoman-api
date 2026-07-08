namespace SafeWoman.Models;

// M-03: decimal en ambas capas — mismo tipo que la API (DECIMAL(10,7) en BD)
public record ActivarSosRequest(decimal Latitud, decimal Longitud);

public class AlertaSosDto
{
    public int      IdAlerta           { get; set; }
    public string   NombreVictima      { get; set; } = default!;
    public decimal  Latitud            { get; set; }
    public decimal  Longitud           { get; set; }
    public DateTime TimestampActivacion { get; set; }
    public string   Estado             { get; set; } = default!;
}
