using Android.Webkit;
using Microsoft.Maui.Handlers;
using AWebView = Android.Webkit.WebView;

namespace SafeWoman.Platforms.Android;

/// Handler personalizado del WebView Android.
/// MAUI carga HtmlWebViewSource con base URL "file:///android_asset/". Desde ese
/// contexto file://, Android WebView bloquea recursos HTTPS externos (Leaflet CDN,
/// tiles CartoDB) a menos que habilitemos AllowUniversalAccessFromFileURLs.
public class CustomWebViewHandler : WebViewHandler
{
    protected override AWebView CreatePlatformView()
    {
        var view = base.CreatePlatformView();

        var settings = view.Settings;
        settings.JavaScriptEnabled                = true;
        settings.DomStorageEnabled                = true;
        settings.AllowUniversalAccessFromFileURLs = true;
        settings.AllowFileAccessFromFileURLs      = true;
        settings.MixedContentMode                 = MixedContentHandling.AlwaysAllow;

        // Zoom nativo del WebView desactivado — el mapa (Leaflet) maneja su propio pinch/zoom.
        // Si lo dejamos activado, se solapan los gestos: el WebView hace zoom del layout
        // Y el mapa hace zoom del contenido → doble zoom, movimiento errático.
        settings.SetSupportZoom(false);
        settings.BuiltInZoomControls              = false;
        settings.DisplayZoomControls              = false;

        // Viewport correcto para que el <meta viewport> del HTML se respete tal cual
        // y el pinch-zoom del mapa Leaflet responda bien en pantallas de alta densidad.
        settings.UseWideViewPort                  = true;
        settings.LoadWithOverviewMode             = false;

        return view;
    }
}
