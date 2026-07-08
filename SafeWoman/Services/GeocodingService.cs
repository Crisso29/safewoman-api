using System.Globalization;
using System.Text.Json;

namespace SafeWoman.Services;

/// Servicio de geocodificación que usa Nominatim/OSM.
/// No usa Geocoding.Default: el Android Geocoder puede bloquear el UI thread causando ANR.
public class GeocodingService
{
    // HttpClient estático: Nominatim exige User-Agent identificado
    private static readonly HttpClient _nominatim = new()
    {
        BaseAddress = new Uri("https://nominatim.openstreetmap.org/"),
        Timeout     = TimeSpan.FromSeconds(8)
    };

    static GeocodingService()
    {
        _nominatim.DefaultRequestHeaders.UserAgent.ParseAdd(
            "SafeWoman/1.0 (UNSCH academic project; crisologo.aguilar.27@unsch.edu.pe)");
        _nominatim.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    // Nominatim (instancia pública) exige máximo 1 request/segundo por User-Agent.
    // Serializamos con SemaphoreSlim y separamos los requests por al menos 1100 ms
    // para no ser bloqueados con HTTP 429 (Too Many Requests).
    private static readonly SemaphoreSlim _rateLimit = new(1, 1);
    private static DateTime _ultimaPeticion = DateTime.MinValue;
    private const int MinIntervaloMs = 1100;

    private static async Task ThrottleAsync(CancellationToken ct)
    {
        await _rateLimit.WaitAsync(ct);
        try
        {
            var espera = MinIntervaloMs - (int)(DateTime.UtcNow - _ultimaPeticion).TotalMilliseconds;
            if (espera > 0) await Task.Delay(espera, ct);
            _ultimaPeticion = DateTime.UtcNow;
        }
        finally
        {
            _rateLimit.Release();
        }
    }

    public record GeoResultado(double Latitud, double Longitud, string? NombreLugar);

    /// Geocodifica con Nominatim/OSM.
    /// No usa Geocoding.Default (Android Geocoder) porque ejecuta en el UI thread y causa ANR.
    public Task<GeoResultado?> BuscarAsync(string referencia, CancellationToken ct = default)
        => BuscarNominatimAsync(referencia, ct);

    /// Devuelve hasta ~8 sugerencias para autocomplete mientras el usuario tipea.
    /// Estrategia doble para maximizar los aciertos con queries reales del usuario:
    ///   1. Búsqueda con la query completa (matches directos).
    ///   2. Si la query completa da menos de 3 hits, DESCOMPONER en palabras clave
    ///      (>= 4 chars, sin stopwords) y buscar cada una por separado. Combinar.
    /// Esto resuelve el caso típico: "losa Wari accopampa ayacucho" → la query
    /// completa da vacío, pero "Wari" y "Accopampa" por separado sí tienen resultados
    /// reales en Ayacucho.
    public async Task<IReadOnlyList<GeoResultado>> SugerirAsync(string prefijo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefijo) || prefijo.Trim().Length < 3)
            return Array.Empty<GeoResultado>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(6));

        var refClean       = prefijo.Trim();
        var acumulado      = new List<GeoResultado>();
        var vistos         = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Query completa
        await BuscarYAcumular(refClean, acumulado, vistos, limit: 6, cts.Token);

        // 2. Si no tenemos suficientes, descomponemos en palabras clave.
        if (acumulado.Count < 3)
        {
            var palabras = ExtraerPalabrasClave(refClean);
            // Quitamos "ayacucho" de las palabras a buscar (todas nuestras búsquedas
            // ya están limitadas al depto Ayacucho por el bbox y filtro display_name).
            palabras = palabras.Where(p =>
                !p.Equals("ayacucho", StringComparison.OrdinalIgnoreCase) &&
                !p.Equals("huamanga", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var palabra in palabras)
            {
                if (acumulado.Count >= 8) break;
                await BuscarYAcumular(palabra, acumulado, vistos, limit: 3, cts.Token);
            }
        }
        return acumulado;
    }

    private async Task BuscarYAcumular(string query, List<GeoResultado> acumulado,
                                       HashSet<string> vistos, int limit, CancellationToken ct)
    {
        try
        {
            var q   = Uri.EscapeDataString(query);
            var url = $"search?q={q}&format=json&limit={limit}&accept-language=es" +
                      $"&countrycodes=pe&addressdetails=0" +
                      $"&viewbox={AyacuchoViewBox}&bounded=1";

            await ThrottleAsync(ct);
            var json = await _nominatim.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() == 0) return;

            foreach (var el in arr.EnumerateArray())
            {
                var lat = double.Parse(el.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
                var lng = double.Parse(el.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
                if (!EstaDentroDeAyacucho(lat, lng)) continue;
                var nombre = el.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
                if (!DisplayNameEnAyacucho(nombre)) continue;

                // Deduplicación por coordenadas (mismo lugar puede aparecer en dos búsquedas).
                var key = $"{lat:F5},{lng:F5}";
                if (!vistos.Add(key)) continue;

                acumulado.Add(new GeoResultado(lat, lng, nombre));
            }
        }
        catch { /* red o parse; sigue con siguiente palabra */ }
    }

    /// Geocodificación inversa: de coordenadas a nombre de lugar.
    public async Task<string?> ReversoAsync(double lat, double lng, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(6));

            var latS = lat.ToString(CultureInfo.InvariantCulture);
            var lngS = lng.ToString(CultureInfo.InvariantCulture);
            var url  = $"reverse?lat={latS}&lon={lngS}&format=json&accept-language=es&zoom=18";

            await ThrottleAsync(cts.Token);
            var json = await _nominatim.GetStringAsync(url, cts.Token);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("display_name", out var dn)
                ? dn.GetString()
                : null;
        }
        catch { return null; }
    }

    // Bounding box del departamento de Ayacucho, Perú.
    // Ajustado para cubrir desde Huanta (extremo norte, aprox -12.34) hasta
    // Sara Sara (extremo sur, aprox -15.55), y de este a oeste todo el departamento.
    // Con bounded=1 en Nominatim se restringe estrictamente la búsqueda a esta zona;
    // el mapa Leaflet también usa estos límites con setMaxBounds para impedir el pan
    // fuera del departamento (evita el caso "Jirón Ayacucho en Huancayo").
    private const string AyacuchoViewBox = "-75.2,-12.3,-73.0,-15.6";
    public  const double AyacuchoLatMin  = -15.6;
    public  const double AyacuchoLatMax  = -12.3;
    public  const double AyacuchoLngMin  = -75.2;
    public  const double AyacuchoLngMax  = -73.0;

    /// True si la coordenada cae dentro del bounding box del departamento de Ayacucho.
    public static bool EstaDentroDeAyacucho(double lat, double lng) =>
        lat >= AyacuchoLatMin && lat <= AyacuchoLatMax &&
        lng >= AyacuchoLngMin && lng <= AyacuchoLngMax;

    // Departamentos vecinos que caen dentro del rectángulo del bounding box.
    // Nominatim SIEMPRE incluye la jerarquía completa en display_name
    // (p. ej. "Chanca Centro, Colca, Huancayo, Junín, Perú").
    // Si el display_name menciona alguno de estos, sabemos que NO está en Ayacucho.
    private static readonly string[] DepartamentosVecinos =
        ["junin", "ica", "cusco", "huancavelica", "apurimac", "lima", "arequipa"];

    /// Valida que el resultado de Nominatim realmente sea del departamento de Ayacucho
    /// exigiendo que:
    ///   (1) la palabra "ayacucho" aparezca en display_name, Y
    ///   (2) ningún departamento vecino aparezca en display_name.
    /// Comparación case-insensitive y sin acentos.
    public static bool DisplayNameEnAyacucho(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return false;
        var normalizado = QuitarAcentos(displayName).ToLowerInvariant();

        if (!normalizado.Contains("ayacucho"))
            return false;

        foreach (var vecino in DepartamentosVecinos)
        {
            if (normalizado.Contains(vecino))
                return false;
        }
        return true;
    }

    private static async Task<GeoResultado?> BuscarNominatimAsync(string referencia, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(7));

        var refClean = referencia.Trim();
        var palabrasClave = ExtraerPalabrasClave(refClean);

        // Estrategia de intentos, de más específica a más laxa:
        //   1. query completa + "Ayacucho, Huamanga"
        //   2. query completa + "Ayacucho"
        //   3. cada segmento útil separado por coma + "Ayacucho"
        //   4. la query completa a secas
        //   5. cada PALABRA clave individual (>= 4 chars, sin stopwords) + "Ayacucho"
        //      → esto rescata casos como "losa Wari accopampa" donde la query completa
        //        no está en OSM pero "Wari" y "Accopampa" sí existen por separado.
        var intentos = new List<string>
        {
            $"{refClean}, Ayacucho, Huamanga",
            $"{refClean}, Ayacucho"
        };

        var partes = refClean.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var parte in partes)
        {
            if (parte.Length >= 4)
                intentos.Add($"{parte}, Ayacucho");
        }
        intentos.Add(refClean);

        // Cada palabra clave individual, para rescatar cuando la frase completa no existe.
        foreach (var palabra in palabrasClave.Where(p =>
                     !p.Equals("ayacucho", StringComparison.OrdinalIgnoreCase) &&
                     !p.Equals("huamanga", StringComparison.OrdinalIgnoreCase)))
        {
            intentos.Add($"{palabra}, Ayacucho");
        }

        foreach (var query in intentos.Distinct())
        {
            var geo = await TryNominatimAsync(query, cts.Token);
            // Cuádruple filtro para garantizar un resultado real de Ayacucho:
            //  1. dentro del bounding box del departamento,
            //  2. el display_name contiene "ayacucho" y ningún departamento vecino
            //     (evita "Chanca Centro, Colca, Huancayo, Junín" que cae en el rect.),
            //  3. el display_name comparte al menos una palabra clave con la query
            //     (evita "Camen Alto" → "Plaza de Armas", falso positivo puro).
            if (geo is not null
                && EstaDentroDeAyacucho(geo.Latitud, geo.Longitud)
                && DisplayNameEnAyacucho(geo.NombreLugar)
                && CoincideAlgunaPalabraClave(geo.NombreLugar, palabrasClave))
                return geo;
        }
        return null;
    }

    /// Extrae palabras clave de la query original que consideramos "significativas"
    /// para validar el match: mínimo 4 caracteres, sin stopwords típicas.
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "calle", "jiron", "jirón", "avenida", "av", "jr", "psje", "pasaje",
        "plaza", "parque", "mercado", "de", "del", "la", "las", "los",
        "el", "en", "por", "para", "con", "sin", "una", "uno",
        "cuadra", "manzana", "lote", "n", "no", "num", "numero", "número",
        "cerca", "casa", "esquina", "frente", "junto"
    };

    private static List<string> ExtraerPalabrasClave(string texto)
    {
        var normalizado = QuitarAcentos(texto).ToLowerInvariant();
        return normalizado
            .Split(new[] { ' ', ',', '.', '-', '/', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= 4 && !Stopwords.Contains(p) && !p.All(char.IsDigit))
            .Distinct()
            .ToList();
    }

    /// True si:
    ///   - no hay palabras clave (query muy corta o toda stopwords) → aceptar todo;
    ///   - al menos una palabra clave aparece dentro del displayName devuelto por Nominatim
    ///     (comparación normalizada sin acentos e insensible a mayúsculas).
    private static bool CoincideAlgunaPalabraClave(string? displayName, List<string> palabrasClave)
    {
        if (palabrasClave.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(displayName)) return false;

        var displayNorm = QuitarAcentos(displayName).ToLowerInvariant();
        return palabrasClave.Any(p => displayNorm.Contains(p));
    }

    /// Normaliza acentos y ñ para comparaciones fuzzy: "Andrés" → "Andres", "Cañete" → "Canete".
    private static string QuitarAcentos(string s)
    {
        var norm = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb   = new System.Text.StringBuilder(norm.Length);
        foreach (var c in norm)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static async Task<GeoResultado?> TryNominatimAsync(string query, CancellationToken ct)
    {
        try
        {
            var q   = Uri.EscapeDataString(query);
            var url = $"search?q={q}&format=json&limit=1&accept-language=es" +
                      $"&countrycodes=pe&addressdetails=0" +
                      $"&viewbox={AyacuchoViewBox}&bounded=1";

            await ThrottleAsync(ct);
            var json = await _nominatim.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() == 0) return null;

            var first  = arr[0];
            var lat    = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            var lng    = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
            var nombre = first.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;

            return new GeoResultado(lat, lng, nombre);
        }
        catch
        {
            return null;
        }
    }
}
