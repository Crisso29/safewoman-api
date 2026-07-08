using SafeWoman.ViewModels.Home;

namespace SafeWoman.Views.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarDatos();
        // Fire-and-forget explícito: la carga de contactos es opcional y usa caché.
        // No bloqueamos OnAppearing con await para que la página aparezca de inmediato.
        _ = _vm.CargarContactosAsync();
    }
}
