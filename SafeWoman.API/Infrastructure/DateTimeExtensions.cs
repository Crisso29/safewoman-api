namespace SafeWoman.API.Infrastructure;

/// Conversión UTC → hora de Lima (UTC-5, sin DST) para las vistas del panel admin.
///
/// Motivo: la BD guarda todo en UTC (best practice), pero antes las vistas usaban
/// .ToLocalTime() — que depende del TimeZone del proceso. En Render/Docker el
/// TimeZone del contenedor es UTC, por lo que .ToLocalTime() era NO-OP y se
/// mostraba UTC como si fuera hora de Lima → desfase de +5 h.
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo LimaZone = LoadLimaZone();

    private static TimeZoneInfo LoadLimaZone()
    {
        // .NET 8 acepta IDs IANA en cualquier plataforma vía ICU; en Windows nativo
        // también aceptamos el ID legacy. Fallback offset fijo si el sistema no
        // tiene la base de datos de zonas horarias (Perú no observa DST).
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Lima", out var iana))
            return iana;
        if (TimeZoneInfo.TryFindSystemTimeZoneById("SA Pacific Standard Time", out var win))
            return win;
        return TimeZoneInfo.CreateCustomTimeZone(
            id: "Peru",
            baseUtcOffset: TimeSpan.FromHours(-5),
            displayName: "Hora estándar de Perú",
            standardDisplayName: "PET");
    }

    /// Convierte cualquier DateTime a la hora local de Lima.
    /// - Si Kind = Utc         → conversión directa.
    /// - Si Kind = Local       → primero pasa a UTC y luego a Lima.
    /// - Si Kind = Unspecified → se asume UTC (los timestamps que vienen de
    ///   Npgsql para columnas "timestamp without time zone" llegan así, y en
    ///   este proyecto SIEMPRE se guardan en UTC).
    public static DateTime ToLimaTime(this DateTime dt)
    {
        var utc = dt.Kind switch
        {
            DateTimeKind.Utc         => dt,
            DateTimeKind.Local       => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _                        => dt
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, LimaZone);
    }

    /// Ahora mismo en Lima. Reemplazo de DateTime.Now que en servidores UTC
    /// devolvería la hora en UTC.
    public static DateTime AhoraLima() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaZone);
}
