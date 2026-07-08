using SafeWoman.ViewModels.Contacto;

namespace SafeWoman.Views.Contacto;

public partial class ContactosPage : ContentPage
{
    private readonly ContactosViewModel _vm;
    private bool _cargando;

    public ContactosPage(ContactosViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Guard contra reentrada: si el usuario navega rápido entre pestañas
        // OnAppearing puede dispararse varias veces antes de que la carga
        // anterior termine. Ignoramos las llamadas superpuestas.
        if (_cargando) return;
        _cargando = true;
        try
        {
            await _vm.CargarAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactosPage.OnAppearing] {ex}");
        }
        finally
        {
            _cargando = false;
        }
    }

    private async void VolverClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactosPage.VolverClicked] {ex}");
        }
    }
}
