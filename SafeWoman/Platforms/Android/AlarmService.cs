using Android.Media;
using SafeWoman.Services;
using AndroidContext = Android.Content.Context;

namespace SafeWoman.Platforms.Android;

/// <summary>
/// Genera la alarma SOS con ToneGenerator — no depende de archivos de sonido del dispositivo.
/// Un timer dispara ráfagas cada 1.2 s hasta que se llame StopAlarm().
/// </summary>
public class AlarmService : IAlarmService
{
    private ToneGenerator?              _toneGen;
    private System.Threading.Timer?    _timer;
    private volatile bool              _active;

    public void StartAlarm()
    {
        StopAlarm();                        // limpia cualquier instancia anterior
        try
        {
            _active = true;

            // Sube el volumen de alarma al máximo
            var audioManager = Platform.AppContext
                .GetSystemService(AndroidContext.AudioService) as AudioManager;
            if (audioManager is not null)
            {
                var max = audioManager.GetStreamMaxVolume(global::Android.Media.Stream.Alarm);
                audioManager.SetStreamVolume(global::Android.Media.Stream.Alarm, max, 0);
            }

            _toneGen = new ToneGenerator(global::Android.Media.Stream.Alarm, 100); // 100 = MAX_VOLUME

            // Primera ráfaga inmediata
            PlayBurst();

            // Repite cada 1 200 ms
            _timer = new System.Threading.Timer(_ => PlayBurst(), null, 1200, 1200);
        }
        catch
        {
            // Si ToneGenerator falla, no interrumpir el flujo SOS
        }
    }

    private void PlayBurst()
    {
        if (!_active || _toneGen is null) return;
        try
        {
            // Tone 89 = CDMA_EMERGENCY_RINGBACK — tono de emergencia estándar de Android
            _toneGen.StartTone((Tone)89, 1000);
        }
        catch { }
    }

    public void StopAlarm()
    {
        _active = false;

        _timer?.Dispose();
        _timer = null;

        try { _toneGen?.StopTone(); } catch { }
        try { _toneGen?.Release();  } catch { }
        _toneGen = null;
    }
}
