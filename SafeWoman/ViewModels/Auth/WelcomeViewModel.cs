using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Views.Auth;

namespace SafeWoman.ViewModels.Auth;

public partial class WelcomeViewModel : ObservableObject
{
    [RelayCommand]
    private async Task IrALogin() =>
        await Shell.Current.GoToAsync(nameof(LoginPage));

    [RelayCommand]
    private async Task IrARegistro() =>
        await Shell.Current.GoToAsync(nameof(RegisterPage));

    [RelayCommand]
    private async Task IrADenunciaAnonima() =>
        await Shell.Current.GoToAsync("DenunciaAnonimaPage");
}
