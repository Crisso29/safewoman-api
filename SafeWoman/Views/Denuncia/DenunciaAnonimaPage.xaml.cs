using System.Globalization;
using SafeWoman.ViewModels.Denuncia;

namespace SafeWoman.Views.Denuncia;

public partial class DenunciaAnonimaPage : ContentPage
{
    private readonly DenunciaAnonimaViewModel _vm;
    private bool _handlerSuscrito;

    public DenunciaAnonimaPage(DenunciaAnonimaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    private void OnMapaNavegando(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("safewoman://ubicacion", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var (lat, lng) = ParsearCoordenadas(e.Url);
            if (lat is not null && lng is not null)
                _ = _vm.ActualizarDesdeMapaAsync(lat.Value, lng.Value);
        }
        else if (e.Url.StartsWith("safewoman://gps", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            _ = _vm.CentrarEnGpsAsync();
        }
        else if (e.Url.StartsWith("safewoman://modo", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            _vm.SetModoSatelite(e.Url.Contains("sat=1", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static (double? lat, double? lng) ParsearCoordenadas(string url)
    {
        var query = url.Contains('?') ? url[(url.IndexOf('?') + 1)..] : string.Empty;
        double? lat = null, lng = null;
        foreach (var par in query.Split('&'))
        {
            var kv = par.Split('=');
            if (kv.Length != 2) continue;
            if (kv[0] == "lat" && double.TryParse(kv[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var la)) lat = la;
            if (kv[0] == "lng" && double.TryParse(kv[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var ln)) lng = ln;
        }
        return (lat, lng);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_handlerSuscrito)
        {
            MapaWebView.Navigating += OnMapaNavegando;
            _handlerSuscrito = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_handlerSuscrito)
        {
            MapaWebView.Navigating -= OnMapaNavegando;
            _handlerSuscrito = false;
        }
    }
}
