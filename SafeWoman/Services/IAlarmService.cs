namespace SafeWoman.Services;

/// <summary>
/// Reproduce y detiene la alarma sonora local del dispositivo durante una alerta SOS.
/// </summary>
public interface IAlarmService
{
    void StartAlarm();
    void StopAlarm();
}
