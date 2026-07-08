using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Models;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Home;

public partial class HomeViewModel : ObservableObject
{
    private readonly AuthStateService         _authState;
    private readonly ApiService               _api;
    private readonly LocationService          _location;
    private readonly IAlarmService            _alarm;
    private readonly DeviceFingerprintService _device;

    [ObservableProperty] private string _nombreVictima    = string.Empty;
    [ObservableProperty] private string _iniciales         = string.Empty;
    [ObservableProperty] private int    _cantidadContactos;
    [ObservableProperty] private bool   _isBusy;

    // ── Seguimiento de denuncias ─────────────────────────────────────────────
    public ObservableCollection<DenunciaResumenItem> MisDenuncias { get; } = [];

    [ObservableProperty] private bool _cargandoDenuncias;
    [ObservableProperty] private bool _tieneDenuncias;
    [ObservableProperty] private bool _sinDenuncias;

    /// <summary>Estado del RefreshView — enlazado a IsRefreshing en el XAML.</summary>
    [ObservableProperty] private bool _refrescandoDenuncias;

    /// <summary>Error visible en la UI si la carga falló (401, red, etc.).</summary>
    [ObservableProperty] private bool _errorDenuncias;
    [ObservableProperty] private string _mensajeErrorDenuncias = string.Empty;

    public HomeViewModel(AuthStateService authState, ApiService api,
                         LocationService location, IAlarmService alarm,
                         DeviceFingerprintService device)
    {
        _authState = authState;
        _api       = api;
        _location  = location;
        _alarm     = alarm;
        _device    = device;
    }

    public void CargarDatos()
    {
        NombreVictima = _authState.NombreCompleto ?? "Usuario";
        Iniciales     = _authState.GetInitials();
    }

    public async Task CargarContactosAsync()
    {
        // Solo llama a la API si los contactos pueden haber cambiado
        if (!_authState.ContactosCacheInvalidado) return;

        var result = await _api.ListarContactosAsync();
        if (result.Success && result.Data is not null)
        {
            CantidadContactos = result.Data.Count;
            _authState.ContactosCacheInvalidado = false;
        }
    }

    /// <summary>
    /// Carga las denuncias del usuario para mostrar el estado en la Home.
    /// Combina formales (por JWT) y anónimas (por device fingerprint) en una
    /// sola lista ordenada por fecha descendente.
    ///
    /// Solo víctimas AUTENTICADAS ven esta sección — es una decisión de diseño
    /// para no exponer denuncias anónimas a alguien que agarre un teléfono ajeno.
    /// </summary>
    public async Task CargarMisDenunciasAsync()
    {
        // Regla de negocio: solo víctimas autenticadas pueden ver el estado.
        if (string.IsNullOrEmpty(_authState.CachedToken))
        {
            MisDenuncias.Clear();
            TieneDenuncias = false;
            SinDenuncias   = false;
            ErrorDenuncias = false;
            return;
        }

        CargandoDenuncias = true;
        ErrorDenuncias    = false;   // Reset del estado de error al reintentar
        try
        {
            var items       = new List<DenunciaResumenItem>();
            var errores     = new List<string>();

            // 1. Formales — mediante JWT.
            var formales = await _api.ObtenerMisDenunciasAsync();
            if (formales.Success && formales.Data is not null)
            {
                items.AddRange(formales.Data.Select(d => new DenunciaResumenItem
                {
                    Id          = d.IdDenuncia,
                    EsAnonima   = false,
                    Estado      = d.Estado,
                    Fecha       = d.FechaEnvio,
                    Descripcion = d.Descripcion
                }));
            }
            else if (!string.IsNullOrEmpty(formales.Error))
            {
                errores.Add($"Formales: {formales.Error}");
            }

            // 2. Anónimas — mediante device fingerprint.
            var fingerprint = _device.GetOrCreate();
            var anonimas = await _api.ObtenerMisDenunciasAnonimasAsync(fingerprint);
            if (anonimas.Success && anonimas.Data is not null)
            {
                items.AddRange(anonimas.Data.Select(d => new DenunciaResumenItem
                {
                    Id          = d.IdDenunciaAnonima,
                    EsAnonima   = true,
                    Estado      = d.Estado,
                    Fecha       = d.FechaEnvio,
                    Descripcion = d.Descripcion
                }));
            }
            else if (!string.IsNullOrEmpty(anonimas.Error))
            {
                errores.Add($"Anónimas: {anonimas.Error}");
            }

            // Ordenamos por fecha descendente (más reciente primero).
            MisDenuncias.Clear();
            foreach (var item in items.OrderByDescending(x => x.Fecha))
                MisDenuncias.Add(item);

            // Decisión de qué estado mostrar:
            //  - Si tenemos items → mostrarlos (aunque una de las llamadas haya fallado).
            //  - Si ambas llamadas fallaron (0 items + 2 errores) → mostrar error.
            //  - Si ambas fueron OK pero no hay items → "sin denuncias".
            if (MisDenuncias.Count > 0)
            {
                TieneDenuncias = true;
                SinDenuncias   = false;
                ErrorDenuncias = false;
            }
            else if (errores.Count == 2)
            {
                TieneDenuncias = false;
                SinDenuncias   = false;
                ErrorDenuncias = true;
                MensajeErrorDenuncias = string.Join(" · ", errores);
            }
            else
            {
                TieneDenuncias = false;
                SinDenuncias   = true;
                ErrorDenuncias = false;
            }
        }
        finally
        {
            CargandoDenuncias = false;
        }
    }

    /// <summary>
    /// Command para el pull-to-refresh del RefreshView.
    /// Reutiliza la misma lógica de carga; solo apaga el spinner al terminar.
    /// </summary>
    [RelayCommand]
    private async Task RefrescarDenunciasAsync()
    {
        try
        {
            await CargarMisDenunciasAsync();
        }
        finally
        {
            RefrescandoDenuncias = false;
        }
    }

    [RelayCommand]
    private async Task ActivarSosAsync()
    {
        IsBusy = true;
        try
        {
            var location = await _location.GetCurrentLocationAsync();
            if (location is null)
            {
                await Shell.Current.DisplayAlert("GPS requerido",
                    "Active el GPS de su dispositivo para usar el botón SOS.", "Aceptar");
                return;
            }

            // Arranca la alarma en background — ToneGenerator init bloquea el UI thread en Android
            _ = Task.Run(() => _alarm.StartAlarm());

            var result = await _api.ActivarSosAsync(
                new Models.ActivarSosRequest((decimal)location.Latitude, (decimal)location.Longitude));

            if (result.Success && result.Data is not null)
            {
                // Usa el conteo que ya está en memoria — no hace falta una segunda llamada
                var nContactos = CantidadContactos;
                // InvariantCulture: evita la coma decimal del locale ES-PE ("13,16387" → "13.16387")
                var lat = result.Data.Latitud.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
                var lng = result.Data.Longitud.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
                await Shell.Current.GoToAsync(
                    $"SosActivePage?idAlerta={result.Data.IdAlerta}&lat={lat}&lng={lng}&contactos={nContactos}");
            }
            else
            {
                _alarm.StopAlarm(); // Detiene la alarma si la API rechazó el SOS
                await Shell.Current.DisplayAlert("Error",
                    result.Error ?? "No se pudo enviar la alerta.", "Aceptar");
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task IrADenunciaFormal() =>
        await Shell.Current.GoToAsync("DenunciaFormalPage");

    [RelayCommand]
    private async Task IrADenunciaAnonima() =>
        await Shell.Current.GoToAsync("DenunciaAnonimaPage");

    [RelayCommand]
    private async Task IrAContactos() =>
        await Shell.Current.GoToAsync("ContactosPage");

    // I-04: ⚙ navega al perfil (con opción de logout), no cierra sesión directamente
    [RelayCommand]
    private async Task IrAPerfil() =>
        await Shell.Current.GoToAsync("PerfilPage");
}
