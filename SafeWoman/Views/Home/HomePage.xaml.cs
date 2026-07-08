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
        // Fire-and-forget explícito: cargas opcionales que no deben bloquear la UI.
        // El usuario ve la Home de inmediato; los datos aparecen al completarse.
        _ = _vm.CargarContactosAsync();
        _ = _vm.CargarMisDenunciasAsync();
    }
}
