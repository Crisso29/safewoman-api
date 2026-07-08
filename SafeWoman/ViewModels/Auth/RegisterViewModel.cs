using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Models;
using SafeWoman.Services;
using SafeWoman.Views.Auth;

namespace SafeWoman.ViewModels.Auth;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _dni            = string.Empty;
    [ObservableProperty] private string _telefono       = string.Empty;
    [ObservableProperty] private string _password       = string.Empty;
    // RN-17: Consentimiento informado Ley N° 29733
    [ObservableProperty] private bool   _consentimientoLey29733;
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _errorMessage   = string.Empty;

    public RegisterViewModel(ApiService api) => _api = api;

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreCompleto) || string.IsNullOrWhiteSpace(Dni)
            || string.IsNullOrWhiteSpace(Telefono) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Todos los campos son obligatorios.";
            return;
        }

        if (Dni.Length != 8 || !Dni.All(char.IsDigit))
        {
            ErrorMessage = "El DNI debe tener 8 dígitos numéricos.";
            return;
        }

        if (Telefono.Length != 9 || !Telefono.All(char.IsDigit))
        {
            ErrorMessage = "El celular debe tener 9 dígitos.";
            return;
        }

        // RN-17 — Ley N° 29733: consentimiento informado obligatorio
        if (!ConsentimientoLey29733)
        {
            ErrorMessage = "Debe aceptar el tratamiento de sus datos personales para continuar.";
            return;
        }

        IsBusy = true;
        try
        {
            var req    = new RegistroRequest(NombreCompleto.Trim(), Dni.Trim(), Telefono.Trim(), Password);
            var result = await _api.RegistrarAsync(req);

            if (result.Success)
                await Shell.Current.GoToAsync($"{nameof(OtpPage)}?telefono={Telefono}");
            else
                ErrorMessage = result.Error ?? "Error al registrar. Intente nuevamente.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task IrALoginAsync() =>
        await Shell.Current.GoToAsync(nameof(LoginPage));
}
