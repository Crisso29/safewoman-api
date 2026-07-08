using SafeWoman.ViewModels.Auth;

namespace SafeWoman.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
