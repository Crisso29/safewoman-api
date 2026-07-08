using SafeWoman.ViewModels.Auth;

namespace SafeWoman.Views.Auth;

public partial class OtpPage : ContentPage
{
    public OtpPage(OtpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
