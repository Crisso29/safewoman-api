using SafeWoman.Services;
using SafeWoman.ViewModels.Home;

namespace SafeWoman.Views.Home;

public partial class SosActivePage : ContentPage
{
    private readonly IAlarmService _alarm;

    public SosActivePage(SosActiveViewModel vm, IAlarmService alarm)
    {
        InitializeComponent();
        BindingContext = vm;
        _alarm = alarm;
    }

    // Garantiza que la alarma se detenga si el usuario sale de la pantalla
    // por cualquier vía (back gesture, back button del sistema, etc.)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _alarm.StopAlarm();
    }
}
