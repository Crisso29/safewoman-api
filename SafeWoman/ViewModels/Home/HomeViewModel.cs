using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Home;

public partial class HomeViewModel : ObservableObject
{
    private readonly AuthStateService _authState;
    private readonly ApiService       _api;
    private readonly LocationService  _location;
    private readonly IAlarmService    _alarm;

    [ObservableProperty] private string _nombreVictima    = string.Empty;
    [ObservableProperty] private string _iniciales         = string.Empty;
    [ObservableProperty] private int    _cantidadContactos;
    [ObservableProperty] private bool   _isBusy;

    public HomeViewModel(AuthStateService authState, ApiService api,
                         LocationService location, IAlarmService alarm)
    {
        _authState = authState;
        _api       = api;
        _location  = location;
        _alarm     = alarm;
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
