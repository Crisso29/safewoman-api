using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Denuncia;

public partial class DenunciaFormalViewModel : ObservableObject
{
    private readonly ApiService       _api;
    private readonly AuthStateService _authState;
    private readonly LocationService  _location;
    private readonly GeocodingService _geocoding;

    // Sección 1 — Datos del denunciante (solo lectura, autocompletados desde el perfil)
    [ObservableProperty] private string _nombreDenunciante   = string.Empty;
    [ObservableProperty] private string _dniDenunciante      = string.Empty;
    [ObservableProperty] private string _telefonoDenunciante = string.Empty;
    [ObservableProperty] private string _fotoDniPath         = string.Empty;
    private FileResult? _fotoDniFile;

    // Sección 2 — Datos del denunciado
    [ObservableProperty] private string _nombreAliasDenunciado = string.Empty;
    [ObservableProperty] private string _relacionDenunciado    = string.Empty;

    // Sección 3 — Ubicación del hecho
    [ObservableProperty] private string   _departamento        = "Ayacucho";
    [ObservableProperty] private string   _provincia           = string.Empty;
    [ObservableProperty] private string   _distrito            = string.Empty;
    [ObservableProperty] private string   _referenciaUbicacion = string.Empty;
    [ObservableProperty] private decimal? _latitud;
    [ObservableProperty] private decimal? _longitud;
    [ObservableProperty] private bool          _ubicacionCapturada;
    [ObservableProperty] private bool          _mostrarMapa;
    [ObservableProperty] private WebViewSource? _mapaSource;
    [ObservableProperty] private string?       _direccionResuelta;
    // Persiste la preferencia del usuario entre recargas del mapa.
    private bool _modoSatelite;
    [ObservableProperty] private DateTime _fechaHecho          = DateTime.Today;
    [ObservableProperty] private TimeSpan _horaHecho           = DateTime.Now.TimeOfDay;

    // Sección 4 — Descripción y evidencias
    [ObservableProperty] private string _descripcion      = string.Empty;
    [ObservableProperty] private bool   _declaracionJurada;

    // Estado UI
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;

    // Autocomplete de referencia de ubicación
    [ObservableProperty] private bool _mostrarSugerencias;
    public ObservableCollection<GeocodingService.GeoResultado> Sugerencias { get; } = new();

    private CancellationTokenSource? _sugerenciasCts;
    private bool _saltarProximaBusquedaSugerencias;

    public ObservableCollection<FileResult> Evidencias { get; } = new();

    public List<string> Relaciones { get; } =
        ["Pareja", "Expareja", "Familiar", "Conocido", "Desconocido"];

    public DenunciaFormalViewModel(
        ApiService api, AuthStateService authState,
        LocationService location, GeocodingService geocoding)
    {
        _api       = api;
        _authState = authState;
        _location  = location;
        _geocoding = geocoding;
    }

    public async Task CargarPerfilAsync()
    {
        var perfil = _authState.PerfilCacheado;
        if (perfil is null)
        {
            var result = await _api.ObtenerPerfilAsync();
            if (!result.Success || result.Data is null) return;
            perfil = result.Data;
            _authState.GuardarPerfil(perfil);
        }

        NombreDenunciante   = perfil.NombreCompleto;
        DniDenunciante      = perfil.Dni;
        TelefonoDenunciante = perfil.Telefono;
    }

    // Un solo punto de entrada para la foto del DNI — action sheet estándar de móvil.
    // Ambas opciones se invocan en el main thread para que Android pueda iniciar
    // la Activity correctamente (CapturePhotoAsync falla en continuaciones de otros threads).
    [RelayCommand]
    private async Task ElegirFotoDniAsync()
    {
        var opcion = await Shell.Current.DisplayActionSheet(
            "Foto del DNI", "Cancelar", null,
            "📷 Tomar foto con la cámara",
            "🖼 Seleccionar de galería");

        if (opcion is null or "Cancelar") return;

        FileResult? resultado = null;

        try
        {
            if (opcion == "📷 Tomar foto con la cámara")
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await Shell.Current.DisplayAlert("Cámara no disponible",
                        "Este dispositivo o emulador no soporta captura de fotos. Use la galería.", "Aceptar");
                    return;
                }

                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permiso de cámara",
                        "Se necesita permiso de cámara para tomar la foto del DNI.\n" +
                        "Vaya a Ajustes → Aplicaciones → SafeWoman → Permisos.", "Aceptar");
                    return;
                }

                // InvokeOnMainThreadAsync garantiza que la Activity de cámara se inicia
                // desde el hilo principal (requisito de Android)
                resultado = await MainThread.InvokeOnMainThreadAsync(
                    () => MediaPicker.Default.CapturePhotoAsync());
            }
            else // galería
            {
                resultado = await MainThread.InvokeOnMainThreadAsync(
                    () => MediaPicker.Default.PickPhotoAsync());
            }
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlert("Permiso requerido",
                "Se necesitan permisos para acceder a la foto. Verifique la configuración de la app.", "Aceptar");
            return;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error al acceder",
                $"No se pudo obtener la foto: {ex.Message}\n\nIntente con la otra opción.", "Aceptar");
            return;
        }

        if (resultado is not null)
        {
            _fotoDniFile = resultado;
            FotoDniPath  = resultado.FileName ?? Path.GetFileName(resultado.FullPath);
        }
    }

    // Límites alineados con la API (SafeWoman.API/DenunciaController):
    // 5 archivos × 10 MB c/u, cualquier imagen/vídeo/audio/PDF que aceptan
    // apps como WhatsApp o Drive. Se validan aquí para no gastar red subiendo
    // algo que el servidor rechazaría.
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
                    continue; // ya estaba, silencioso
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
            return 0; // si no se puede leer, dejamos que la API lo valide
        }
    }

    [RelayCommand]
    private void QuitarEvidencia(FileResult file) => Evidencias.Remove(file);

    // Centro por defecto: Plaza Mayor de Huamanga (Ayacucho).
    // Se usa como fallback cuando Nominatim no encuentra el lugar y el GPS falla
    // (típico en emulador). El usuario abre el mapa ahí y mueve el pin al lugar exacto.
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

            // 1. Intento con geocoder (Nominatim + filtros).
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

            // 2. Si no encontró, intento con GPS actual del dispositivo.
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

            // 3. Fallback: centrar en Huamanga y dejar que el usuario mueva el pin.
            //    Preferible a bloquearlo con un alert cuando OSM simplemente no tiene el lugar.
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
                Html = MapaHtmlBuilder.Build(lat, lng, MapaHtmlBuilder.ColorFormal, _modoSatelite)
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

        if (_fotoDniFile is null)
        { ErrorMessage = "La foto del DNI es obligatoria."; return; }

        if (string.IsNullOrWhiteSpace(Descripcion))
        { ErrorMessage = "La descripción es obligatoria."; return; }

        if (!DeclaracionJurada)
        { ErrorMessage = "Debe aceptar la declaración jurada para continuar."; return; }

        IsBusy = true;

        // Rastreamos streams manualmente — evita crash en Android por dispose de URIs de contenido
        var streams = new List<Stream>();
        MultipartFormDataContent? form = null;
        bool enviado = false;

        try
        {
            form = new MultipartFormDataContent();

            // Datos del denunciado — solo si el usuario ingresó algo (campo vacío → no enviar,
            // porque el API espera RelacionVictima? enum y una cadena vacía causaría un 400)
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
                form.Add(new StringContent(Latitud.Value.ToString(CultureInfo.InvariantCulture)),  "Latitud");
            if (Longitud.HasValue)
                form.Add(new StringContent(Longitud.Value.ToString(CultureInfo.InvariantCulture)), "Longitud");

            form.Add(new StringContent(FechaHecho.ToString("yyyy-MM-dd")),   "FechaHecho");
            form.Add(new StringContent(HoraHecho.ToString(@"hh\:mm")),       "HoraHecho");
            form.Add(new StringContent(Descripcion ?? ""),                    "Descripcion");

            // Foto DNI — OpenReadAsync soporta content URIs de Android.
            // Siempre seteamos ContentType desde la extensión: MediaPicker de Android
            // a veces envía cadena vacía o "application/octet-stream" genérico y el
            // server lo rechazaría.
            var fotoStream  = await _fotoDniFile.OpenReadAsync();
            streams.Add(fotoStream);
            var fotoContent = new StreamContent(fotoStream);
            fotoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                InferContentType(_fotoDniFile));
            form.Add(fotoContent, "FotoDni", _fotoDniFile.FileName);

            foreach (var ev in Evidencias)
            {
                var stream  = await ev.OpenReadAsync();
                streams.Add(stream);
                var content = new StreamContent(stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    InferContentType(ev));
                form.Add(content, "Evidencias", ev.FileName);
            }

            var result = await _api.EnviarDenunciaFormalAsync(form);
            if (result.Success)
            {
                enviado = true;
                await Shell.Current.DisplayAlert("✅ Denuncia enviada",
                    "Su denuncia formal fue enviada exitosamente. Un operador la atenderá pronto.", "Aceptar");
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
            var msg = $"Error al preparar el envío: {ex.GetType().Name} — {ex.Message}";
            ErrorMessage = msg;
            await Shell.Current.DisplayAlert("Error al enviar", msg, "Aceptar");
        }
        finally
        {
            foreach (var s in streams)
                try { s.Dispose(); } catch { /* ignorar */ }
            try { form?.Dispose(); } catch { /* ignorar */ }

            IsBusy = false;
        }

        if (enviado)
            await Shell.Current.GoToAsync("//HomePage");
    }

    [RelayCommand]
    private async Task VolverAsync() => await Shell.Current.GoToAsync("..");

    // Deriva un content-type razonable para MultipartFormDataContent.
    // Prefiere el ContentType del FileResult; si está vacío o es genérico,
    // lo infiere de la extensión.
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

    // Llamado desde el code-behind cuando el usuario mueve el mapa.
    // Actualiza las coordenadas y hace reverse-geocoding para mostrar la nueva dirección.
    // Defensa: si de algún modo las coordenadas caen fuera de Ayacucho (bug del control del
    // mapa, dispositivo raro, etc.), ignoramos el cambio para no marcar denuncias en Junín.
    public async Task ActualizarDesdeMapaAsync(double lat, double lng)
    {
        // Filtro 1: rechazar si está fuera del rectángulo de Ayacucho.
        if (!GeocodingService.EstaDentroDeAyacucho(lat, lng))
            return;

        DireccionResuelta = "Obteniendo dirección…";
        var nombre = await _geocoding.ReversoAsync(lat, lng);

        // Filtro 2: si el reverse-geocoding dice que el punto pertenece a un
        // departamento vecino (Junín, Ica, Cusco, etc.), rechazar el cambio.
        // El pin visible del mapa queda donde estaba, no se aceptan coords fuera.
        if (nombre is not null && !GeocodingService.DisplayNameEnAyacucho(nombre))
        {
            DireccionResuelta = "Ese punto está fuera de Ayacucho. Mueva el mapa dentro del departamento.";
            return;
        }

        Latitud           = (decimal)lat;
        Longitud          = (decimal)lng;
        DireccionResuelta = nombre ?? $"Lat {lat:F5}, Lng {lng:F5}";
    }

    // Llamado desde el code-behind cuando el usuario alterna entre Calle y Satélite.
    // El estado se conserva y se aplica al próximo Build del HTML del mapa, para que
    // el usuario no pierda su preferencia si el mapa se regenera (GPS, sugerencia…).
    public void SetModoSatelite(bool satelite) => _modoSatelite = satelite;

    // Llamado desde el code-behind cuando el usuario toca el botón GPS del mapa.
    // Toma la ubicación GPS actual, valida que esté en Ayacucho y regenera el mapa
    // centrado ahí. Si el GPS no está disponible o cae fuera del departamento,
    // avisa al usuario en el label de dirección resuelta.
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
            Html = MapaHtmlBuilder.Build(loc.Latitude, loc.Longitude, MapaHtmlBuilder.ColorFormal, _modoSatelite)
        };
    }

    // Autocomplete: se dispara cada vez que ReferenciaUbicacion cambia (el usuario escribe).
    // Debounce 350 ms + cancelación de la búsqueda anterior para no gastar Nominatim en cada tecla.
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
        catch (OperationCanceledException) { /* sustituido por búsqueda más reciente */ }
    }

    // Cuando el usuario elige una sugerencia del dropdown, la aplicamos directo:
    // ni geocoder ni GPS — usamos las coordenadas exactas del resultado de Nominatim.
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
            Html = MapaHtmlBuilder.Build(sug.Latitud, sug.Longitud, MapaHtmlBuilder.ColorFormal, _modoSatelite)
        };
        MostrarMapa = true;
        Sugerencias.Clear();
        MostrarSugerencias = false;
    }
}
