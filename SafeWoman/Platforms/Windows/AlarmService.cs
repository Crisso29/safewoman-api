using SafeWoman.Services;

namespace SafeWoman.Platforms.Windows;

/// <summary>
/// Stub Windows — sin alarma sonora (solo se ejecuta en Android).
/// </summary>
public class AlarmService : IAlarmService
{
    public void StartAlarm() { }
    public void StopAlarm()  { }
}
