using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Denuncia;

public partial class DenunciaAnonimaViewModel : ObservableObject
{
    private readonly ApiService               _api;
    private readonly DeviceFingerprintService _fingerprint;
    private readonly LocationService          _location;
    private readonly GeocodingService         _geocoding;
    private readonly AuthStateService         _authState;

    [ObservableProperty] private string   _nombreAliasDenunciado = string.Empty;
    [ObservableProperty] private string   _relacionDenunciado    = string.Empty;
    [ObservableProperty] private string   _departamento          = "Ayacucho";
    [ObservableProperty] private string   _provincia             = string.Empty;
    [ObservableProperty] private string   _distrito              = string.Empty;
    [ObservableProperty] private string   _referenciaUbicacion   = string.Empty;
    [ObservableProperty] private decimal? _latitud;
    [ObservableProperty] private decimal? _longitud;
    [ObservableProperty] private bool          _ubicacionCapturada;
    [ObservableProperty] private bool          _mostrarMapa;
    [ObservableProperty] private WebViewSource? _mapaSource;
    [ObservableProperty] private string?       _direccionResuelta;
    private bool _modoSatelite;
    [ObservableProperty] private DateTime      _fechaHecho = DateTime.Today;
    [ObservableProperty] private TimeSpan _horaHecho             = DateTime.Now.TimeOfDay;
    [ObservableProperty] private string   _descripcion           = string.Empty;
    [ObservableProperty] private bool     _isBusy;
    [ObservableProperty] private string   _errorMessage          = string.Empty;

    // Autocomplete de referencia de ubicación
    [ObservableProperty] private bool _mostrarSugerencias;
    public ObservableCollection<GeocodingService.GeoResultado> Sugerencias { get; } = new();

    private CancellationTokenSource? _sugerenciasCts;
    private bool _saltarProximaBusquedaSugerencias;

    public ObservableCollection<FileResult> Evidencias { get; } = new();

    public List<string> Relaciones { get; } =
        ["Pareja", "Expareja", "Familiar", "Conocido", "Desconocido"];

    public DenunciaAnonimaViewModel(
        ApiService api,
        DeviceFingerprintService fingerprint,
        LocationService location,
        GeocodingService geocoding,
        AuthStateService authState)
    {
        _api         = api;
        _fingerprint = fingerprint;
        _authState   = authState;
        _location    = location;
        _geocoding   = geocoding;
    }

    // Mismas cotas que en DenunciaFormalViewModel (ver comentario allí).
    private const int  MaxEvidencias      = 5;
    private const long MaxEvidenciaBytes  = 10 * 1024 * 1024;

    [RelayCommand]
    private async Task AgregarEvidenciaAsync()
    {
        try
        {
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Seleccionar evidencias",
                FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["image/*", "video/*", "audio/*", "application/pdf"] },
                    { DevicePlatform.WinUI,   [".jpg", ".jpeg", ".png", ".webp", ".heic",
                                               ".pdf",
                                               ".mp4", ".mov", ".3gp",
                                               ".mp3", ".m4a", ".ogg", ".wav"] },
                    { DevicePlatform.iOS,     ["public.image", "public.movie",
                                               "public.audio", "com.adobe.pdf"] },
                })
            });

            if (results is null) return;

            var rechazadas = new List<string>();
            foreach (var file in results)
            {
                if (Evidencias.Count >= MaxEvidencias)
                {
                    rechazadas.Add($"Máximo {MaxEvidencias} evidencias — no se agregó '{file.FileName}'.");
                    continue;
                }
                var tamano = await ObtenerTamanoAsync(file);
                if (tamano > MaxEvidenciaBytes)
                {
                    rechazadas.Add($"'{file.FileName}' supera 10 MB.");
                    continue;
                }
                if (Evidencias.Any(e => e.FullPath == file.FullPath))
                    continue;
                Evidencias.Add(file);
            }

            if (rechazadas.Count > 0)
                await Shell.Current.DisplayAlert("Evidencias no agregadas",
                    string.Join('\n', rechazadas), "Aceptar");
        }
        catch { /* usuario canceló el picker */ }
    }

    private static async Task<long> ObtenerTamanoAsync(FileResult file)
    {
        try
        {
            using var stream = await file.OpenReadAsync();
            return stream.Length;
        }
        catch
        {
            return 0;
        }
    }

    [RelayCommand]
    private void QuitarEvidencia(FileResult file) => Evidencias.Remove(file);

    // Centro por defecto: Plaza Mayor de Huamanga.
    private const double HuamangaLat = -13.1587;
    private const double HuamangaLng = -74.2264;

    [RelayCommand]
    private async Task CapturarUbicacionAsync()
    {
        IsBusy             = true;
        UbicacionCapturada = false;
        DireccionResuelta  = null;
        try
        {
            double lat = HuamangaLat, lng = HuamangaLng;
            string? nombreLugar = null;
            bool encontrado = false;

            if (!string.IsNullOrWhiteSpace(ReferenciaUbicacion))
            {
                var geo = await _geocoding.BuscarAsync(ReferenciaUbicacion);
                if (geo is not null)
                {
                    lat = geo.Latitud; lng = geo.Longitud;
                    nombreLugar = geo.NombreLugar ?? ReferenciaUbicacion;
                    encontrado = true;
                }
            }

            if (!encontrado)
            {
                var loc = await _location.GetCurrentLocationAsync();
                if (loc is not null && GeocodingService.EstaDentroDeAyacucho(loc.Latitude, loc.Longitude))
                {
                    lat = loc.Latitude; lng = loc.Longitude;
                    nombreLugar = string.IsNullOrWhiteSpace(ReferenciaUbicacion)
                        ? "Coordenadas GPS registradas"
                        : ReferenciaUbicacion;
                    encontrado = true;
                }
            }

            if (!encontrado)
            {
                nombreLugar = string.IsNullOrWhiteSpace(ReferenciaUbicacion)
                    ? "Mueva el mapa al lugar exacto del hecho."
                    : $"No se encontró '{ReferenciaUbicacion}' en OpenStreetMap. Mueva el mapa al lugar exacto.";
            }

            Latitud            = (decimal)lat;
            Longitud           = (decimal)lng;
            DireccionResuelta  = nombreLugar;
            UbicacionCapturada = true;
            MapaSource = new HtmlWebViewSource
            {
                Html = MapaHtmlBuilder.Build(lat, lng, MapaHtmlBuilder.ColorAnonima, _modoSatelite)
            };
            MostrarMapa = true;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AbrirMapaAsync()
    {
        if (!Latitud.HasValue || !Longitud.HasValue) return;
        try
        {
            await Map.Default.OpenAsync(
                new Location((double)Latitud.Value, (double)Longitud.Value),
                new MapLaunchOptions { Name = "Ubicación del hecho" });
        }
        catch
        {
            var lat = Latitud.Value.ToString(CultureInfo.InvariantCulture);
            var lng = Longitud.Value.ToString(CultureInfo.InvariantCulture);
            await Launcher.Default.OpenAsync($"https://maps.google.com/?q={lat},{lng}");
        }
    }

    [RelayCommand]
    private async Task EnviarAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Descripcion))
        { ErrorMessage = "La descripción es obligatoria."; return; }

        IsBusy = true;

        // Rastreamos los streams manualmente para poder cerrarlos de forma segura
        // en el finally aunque el HTTP falle a mitad de lectura (evita crash en Android).
        var streams = new List<Stream>();
        MultipartFormDataContent? form = null;
        bool enviado = false;

        try
        {
            form = new MultipartFormDataContent();
            form.Add(new StringContent(_fingerprint.GetOrCreate()), "DeviceFingerprint");

            // Datos del denunciado — solo si el usuario ingresó algo (vacío → no enviar,
            // porque RelacionVictima? es un enum y una cadena vacía causaría error de binding)
            if (!string.IsNullOrWhiteSpace(NombreAliasDenunciado))
                form.Add(new StringContent(NombreAliasDenunciado.Trim()), "NombreAliasDenunciado");
            if (!string.IsNullOrEmpty(RelacionDenunciado))
                form.Add(new StringContent(RelacionDenunciado),           "RelacionDenunciado");

            // Ubicación
            form.Add(new StringContent(Departamento), "Departamento");
            if (!string.IsNullOrWhiteSpace(Provincia))
                form.Add(new StringContent(Provincia.Trim()), "Provincia");
            if (!string.IsNullOrWhiteSpace(Distrito))
                form.Add(new StringContent(Distrito.Trim()), "Distrito");
            if (!string.IsNullOrWhiteSpace(ReferenciaUbicacion))
                form.Add(new StringContent(ReferenciaUbicacion.Trim()), "ReferenciaUbicacion");
            if (Latitud.HasValue)
                form.Add(new StringContent(Latitud.Value.ToString(CultureInfo.InvariantCulture)), "Latitud");
            if (Longitud.HasValue)
                form.Add(new StringContent(Longitud.Value.ToString(CultureInfo.InvariantCulture)), "Longitud");

            form.Add(new StringContent(FechaHecho.ToString("yyyy-MM-dd")), "FechaHecho");
            form.Add(new StringContent(HoraHecho.ToString(@"hh\:mm")),     "HoraHecho");
            form.Add(new StringContent(Descripcion ?? ""),                  "Descripcion");

            foreach (var ev in Evidencias)
            {
                var stream  = await ev.OpenReadAsync();
                streams.Add(stream);
                var content = new StreamContent(stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    InferContentType(ev));
                form.Add(content, "Evidencias", ev.FileName);
            }

            var result = await _api.EnviarDenunciaAnonimaAsync(form);
            if (result.Success)
            {
                enviado = true;
                await Shell.Current.DisplayAlert("✅ Denuncia enviada",
                    "Su denuncia anónima fue enviada correctamente. Su identidad está protegida.", "Aceptar");
            }
            else
            {
                var msg = result.Error ?? "No se pudo enviar la denuncia. Intente nuevamente.";
                ErrorMessage = msg;
                await Shell.Current.DisplayAlert("No se pudo enviar", msg, "Intentar de nuevo");
            }
        }
        catch (Exception ex)
        {
            // Este catch cubre errores de construcción del form (antes del HTTP call).
            // Los errores del HTTP pipeline ya son atrapados en ApiService.SendAsync.
            var msg = $"Error al preparar el envío: {ex.GetType().Name} — {ex.Message}";
            ErrorMessage = msg;
            await Shell.Current.DisplayAlert("Error al enviar", msg, "Aceptar");
        }
        finally
        {
            // Cerrar streams de forma segura — no deben propagar excepciones aquí
            foreach (var s in streams)
                try { s.Dispose(); } catch { /* ignorar */ }
            try { form?.Dispose(); } catch { /* ignorar */ }

            IsBusy = false;
        }

        // Navegar solo si el envío fue exitoso (fuera del try/finally).
        // Si el usuario tiene sesión activa (víctima autenticada haciendo una anónima),
        // vuelve a HomePage. Si es testigo/anónimo sin sesión, vuelve a WelcomePage.
        if (enviado)
        {
            var destino = _authState.ProbablyLoggedIn ? "//HomePage" : "//WelcomePage";
            await Shell.Current.GoToAsync(destino);
        }
    }

    [RelayCommand]
    private async Task VolverAsync() => await Shell.Current.GoToAsync("..");

    private static string InferContentType(FileResult file)
    {
        var ct = file.ContentType?.Trim();
        if (!string.IsNullOrEmpty(ct) && ct != "application/octet-stream")
            return ct;

        var ext = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".webp"           => "image/webp",
            ".heic"           => "image/heic",
            ".pdf"            => "application/pdf",
            ".mp4"            => "video/mp4",
            ".mov"            => "video/quicktime",
            ".3gp"            => "video/3gpp",
            ".mp3"            => "audio/mpeg",
            ".m4a"            => "audio/mp4",
            ".ogg"            => "audio/ogg",
            ".wav"            => "audio/wav",
            _                 => "application/octet-stream"
        };
    }

    public async Task ActualizarDesdeMapaAsync(double lat, double lng)
    {
        if (!GeocodingService.EstaDentroDeAyacucho(lat, lng))
            return;

        DireccionResuelta = "Obteniendo dirección…";
        var nombre = await _geocoding.ReversoAsync(lat, lng);

        if (nombre is not null && !GeocodingService.DisplayNameEnAyacucho(nombre))
        {
            DireccionResuelta = "Ese punto está fuera de Ayacucho. Mueva el mapa dentro del departamento.";
            return;
        }

        Latitud           = (decimal)lat;
        Longitud          = (decimal)lng;
        DireccionResuelta = nombre ?? $"Lat {lat:F5}, Lng {lng:F5}";
    }

    public void SetModoSatelite(bool satelite) => _modoSatelite = satelite;

    public async Task CentrarEnGpsAsync()
    {
        DireccionResuelta = "Obteniendo tu ubicación GPS…";
        var loc = await _location.GetCurrentLocationAsync();
        if (loc is null)
        {
            DireccionResuelta = "No se pudo obtener el GPS. Verifica los permisos y que el GPS esté activo.";
            return;
        }
        if (!GeocodingService.EstaDentroDeAyacucho(loc.Latitude, loc.Longitude))
        {
            DireccionResuelta = "Tu GPS actual está fuera de Ayacucho. Mueve el mapa manualmente.";
            return;
        }

        Latitud  = (decimal)loc.Latitude;
        Longitud = (decimal)loc.Longitude;
        var nombre = await _geocoding.ReversoAsync(loc.Latitude, loc.Longitude);
        DireccionResuelta = nombre ?? "Tu ubicación GPS actual";
        MapaSource = new HtmlWebViewSource
        {
            Html = MapaHtmlBuilder.Build(loc.Latitude, loc.Longitude, MapaHtmlBuilder.ColorAnonima, _modoSatelite)
        };
    }

    partial void OnReferenciaUbicacionChanged(string value)
    {
        if (_saltarProximaBusquedaSugerencias)
        {
            _saltarProximaBusquedaSugerencias = false;
            return;
        }
        _ = ActualizarSugerenciasAsync(value);
    }

    private async Task ActualizarSugerenciasAsync(string prefijo)
    {
        _sugerenciasCts?.Cancel();
        _sugerenciasCts = new CancellationTokenSource();
        var ct = _sugerenciasCts.Token;
        try
        {
            await Task.Delay(350, ct);
            if (ct.IsCancellationRequested) return;
            var results = await _geocoding.SugerirAsync(prefijo, ct);
            if (ct.IsCancellationRequested) return;
            Sugerencias.Clear();
            foreach (var r in results) Sugerencias.Add(r);
            MostrarSugerencias = Sugerencias.Count > 0;
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private void ElegirSugerencia(GeocodingService.GeoResultado? sug)
    {
        if (sug is null) return;
        _saltarProximaBusquedaSugerencias = true;
        ReferenciaUbicacion = sug.NombreLugar?.Split(',')[0].Trim() ?? ReferenciaUbicacion;
        Latitud            = (decimal)sug.Latitud;
        Longitud           = (decimal)sug.Longitud;
        DireccionResuelta  = sug.NombreLugar;
        UbicacionCapturada = true;
        MapaSource = new HtmlWebViewSource
        {
            Html = MapaHtmlBuilder.Build(sug.Latitud, sug.Longitud, MapaHtmlBuilder.ColorAnonima, _modoSatelite)
        };
        MostrarMapa = true;
        Sugerencias.Clear();
        MostrarSugerencias = false;
    }

}
