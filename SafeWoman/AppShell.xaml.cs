using SafeWoman.Views.Auth;
using SafeWoman.Views.Contacto;
using SafeWoman.Views.Denuncia;
using SafeWoman.Views.Home;
using SafeWoman.Views.Perfil;

namespace SafeWoman;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterPage),     typeof(RegisterPage));
        Routing.RegisterRoute(nameof(LoginPage),        typeof(LoginPage));
        Routing.RegisterRoute(nameof(OtpPage),          typeof(OtpPage));
        Routing.RegisterRoute(nameof(SosActivePage),    typeof(SosActivePage));
        Routing.RegisterRoute("DenunciaFormalPage",     typeof(DenunciaFormalPage));
        Routing.RegisterRoute("DenunciaAnonimaPage",    typeof(DenunciaAnonimaPage));
        Routing.RegisterRoute("ContactosPage",          typeof(ContactosPage));
        Routing.RegisterRoute("PerfilPage",             typeof(PerfilPage));
    }
}
