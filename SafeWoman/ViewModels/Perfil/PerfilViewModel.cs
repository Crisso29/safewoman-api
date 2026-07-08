using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Perfil;

public partial class PerfilViewModel : ObservableObject
{
    private readonly AuthStateService _authState;
    private readonly ApiService       _api;

    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _dni            = string.Empty;
    [ObservableProperty] private string _telefono       = string.Empty;
    [ObservableProperty] private string _iniciales      = string.Empty;
    [ObservableProperty] private bool   _isBusy;

    public PerfilViewModel(AuthStateService authState, ApiService api)
    {
        _authState = authState;
        _api       = api;
    }

    public async Task CargarAsync()
    {
        // Fallback local siempre — se muestra inmediatamente aunque no haya red.
        // Los datos se guardaron en Preferences durante el login.
        Iniciales      = _authState.GetInitials();
        NombreCompleto = _authState.NombreCompleto ?? string.Empty;
        Dni            = _authState.Dni            ?? string.Empty;
        Telefono       = _authState.Telefono       ?? string.Empty;

        // Refresco desde la API — si funciona, sobreescribe con datos frescos.
        // Si falla (token expirado, sin red), quedan los de Preferences.
        var perfil = _authState.PerfilCacheado;
        if (perfil is null)
        {
            var result = await _api.ObtenerPerfilAsync();
            if (result.Success && result.Data is not null)
            {
                perfil = result.Data;
                _authState.GuardarPerfil(perfil);
            }
        }

        if (perfil is not null)
        {
            NombreCompleto = perfil.NombreCompleto;
            Dni            = perfil.Dni;
            Telefono       = perfil.Telefono;
        }
    }

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Cerrar sesión",
            "¿Desea cerrar su sesión?",
            "Sí, salir", "Cancelar");

        if (!confirm) return;

        _authState.ClearSession();
        await Shell.Current.GoToAsync("//WelcomePage");
    }

    [RelayCommand]
    private async Task VolverAsync() => await Shell.Current.GoToAsync("..");
}
