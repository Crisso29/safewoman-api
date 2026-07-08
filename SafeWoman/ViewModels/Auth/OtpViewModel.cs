using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Models;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Auth;

[QueryProperty(nameof(Telefono), "telefono")]
public partial class OtpViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly AuthStateService _authState;

    [ObservableProperty] private string _telefono = string.Empty;
    [ObservableProperty] private string _codigo = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public OtpViewModel(ApiService api, AuthStateService authState)
    {
        _api = api;
        _authState = authState;
    }

    [RelayCommand]
    private async Task VerificarAsync()
    {
        if (string.IsNullOrWhiteSpace(Codigo) || Codigo.Length != 6)
        {
            ErrorMessage = "Ingrese el código de 6 dígitos.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.VerificarOtpAsync(new VerificarOtpRequest(Telefono, Codigo));
            if (result.Success && result.Data is not null)
            {
                await _authState.StoreSessionAsync(result.Data);
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                ErrorMessage = result.Error ?? "Código incorrecto o expirado.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReenviarAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.ReenviarOtpAsync(Telefono);
            if (result.Success)
                await Shell.Current.DisplayAlert("SafeWoman", "Código reenviado a su teléfono.", "Aceptar");
            else
                ErrorMessage = result.Error ?? "No se pudo reenviar el código.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
