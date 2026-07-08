using SafeWoman.ViewModels.Auth;

namespace SafeWoman.Views.Auth;

public partial class WelcomePage : ContentPage
{
    public WelcomePage(WelcomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
