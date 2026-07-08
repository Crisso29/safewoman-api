using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace SafeWoman
{
    // WindowSoftInputMode = AdjustResize hace que el layout se redimensione cuando
    // aparece el teclado, para que el Editor enfocado quede visible sobre él.
    // Sin esto el teclado tapa el Editor y el usuario cree que "no puede escribir".
    [Activity(
        Theme                 = "@style/Maui.SplashTheme",
        MainLauncher          = true,
        LaunchMode            = LaunchMode.SingleTop,
        WindowSoftInputMode   = SoftInput.AdjustResize,
        ConfigurationChanges  = ConfigChanges.ScreenSize | ConfigChanges.Orientation
                              | ConfigChanges.UiMode     | ConfigChanges.ScreenLayout
                              | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
