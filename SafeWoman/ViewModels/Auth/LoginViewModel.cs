using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Models;
using SafeWoman.Services;
using SafeWoman.Views.Auth;

namespace SafeWoman.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService       _api;
    private readonly AuthStateService _authState;

    // RF-04: el campo acepta teléfono (9 dígitos) o DNI (8 dígitos)
    [ObservableProperty] private string _identificador = string.Empty;
    [ObservableProperty] private string _password      = string.Empty;
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _errorMessage  = string.Empty;

    public LoginViewModel(ApiService api, AuthStateService authState)
    {
        _api       = api;
        _authState = authState;
    }

    [RelayCommand]
    private async Task IrARegistro() =>
        await Shell.Current.GoToAsync(nameof(RegisterPage));

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Identificador) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Ingrese su teléfono o DNI y la contraseña.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.LoginAsync(new LoginRequest(Identificador.Trim(), Password));
            if (result.Success && result.Data is not null)
            {
                // Verificar que el server devolvió realmente un token. Si el JSON del server
                // no incluyó "token" por algún error de serialización, no seguimos.
                if (string.IsNullOrWhiteSpace(result.Data.Token))
                {
                    ErrorMessage = "El servidor no devolvió un token. Contacte a soporte.";
                    return;
                }
                await _authState.StoreSessionAsync(result.Data);
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[Login] Token guardado ({result.Data.Token.Length} chars). Navegando a Home.");
#endif
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                ErrorMessage = result.Error ?? "Credenciales incorrectas.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
