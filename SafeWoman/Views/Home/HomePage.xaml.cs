using SafeWoman.ViewModels.Home;

namespace SafeWoman.Views.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;
    private EventHandler? _windowActivatedHandler;

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

        // Auto-refresh cuando la app vuelve del background (usuario minimizó y volvió).
        // Captura el escenario "estaba en el panel Admin cambiando estados en el
        // navegador, vuelvo a la app" — sin obligar al usuario a tirar hacia abajo.
        // Usamos Window.Activated (disponible en MAUI 8) que dispara al recuperar foco.
        if (Window is not null)
        {
            _windowActivatedHandler = (_, _) => _ = _vm.CargarMisDenunciasAsync();
            Window.Activated += _windowActivatedHandler;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Sacar la suscripción al abandonar la Home — evita memory leaks
        // y llamadas innecesarias cuando el usuario está en otras páginas.
        if (Window is not null && _windowActivatedHandler is not null)
            Window.Activated -= _windowActivatedHandler;
        _windowActivatedHandler = null;
    }
}
