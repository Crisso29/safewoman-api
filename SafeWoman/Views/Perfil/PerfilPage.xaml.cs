using SafeWoman.ViewModels.Perfil;

namespace SafeWoman.Views.Perfil;

public partial class PerfilPage : ContentPage
{
    private readonly PerfilViewModel _vm;
    private bool _cargando;

    public PerfilPage(PerfilViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_cargando) return;
        _cargando = true;
        try
        {
            await _vm.CargarAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PerfilPage.OnAppearing] {ex}");
        }
        finally
        {
            _cargando = false;
        }
    }
}
