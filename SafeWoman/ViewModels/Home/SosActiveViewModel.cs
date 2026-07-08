using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Home;

[QueryProperty(nameof(IdAlerta),         "idAlerta")]
[QueryProperty(nameof(LatitudTexto),     "lat")]
[QueryProperty(nameof(LongitudTexto),    "lng")]
[QueryProperty(nameof(CantidadContactos),"contactos")]
public partial class SosActiveViewModel : ObservableObject
{
    private readonly ApiService    _api;
    private readonly IAlarmService _alarm;

    [ObservableProperty] private int  _idAlerta;
    [ObservableProperty] private bool _isCancelling;

    // Notifican a CoordenadasTexto cuando cambian
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoordenadasTexto))]
    private string _latitudTexto = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoordenadasTexto))]
    private string _longitudTexto = string.Empty;

    // Notifica a ContactosTexto cuando cambia
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactosTexto))]
    private int _cantidadContactos;

    // Propiedades computadas — la UI recibe PropertyChanged por las anotaciones anteriores
    public string CoordenadasTexto =>
        string.IsNullOrEmpty(LatitudTexto)
            ? "Ayacucho"
            : $"Lat {LatitudTexto} · Lng {LongitudTexto}";

    public string ContactosTexto =>
        CantidadContactos > 0
            ? $"SMS enviado a {CantidadContactos} contacto(s)"
            : "SMS enviado a tus contactos";

    public SosActiveViewModel(ApiService api, IAlarmService alarm)
    {
        _api   = api;
        _alarm = alarm;
    }

    [RelayCommand]
    private async Task CancelarAlertaAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Cancelar alerta",
            "¿Está segura que desea cancelar la alerta SOS?",
            "Sí, cancelar", "No");

        if (!confirm) return;

        IsCancelling = true;
        try
        {
            _alarm.StopAlarm(); // Detiene la alarma al cancelar el SOS
            await _api.CancelarSosAsync(IdAlerta);
            await Shell.Current.GoToAsync("//HomePage");
        }
        finally { IsCancelling = false; }
    }
}
