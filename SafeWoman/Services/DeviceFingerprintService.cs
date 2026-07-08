namespace SafeWoman.Services;

public class DeviceFingerprintService
{
    private const string FingerprintKey = "device_fingerprint";

    public string GetOrCreate()
    {
        var existing = Preferences.Default.Get<string?>(FingerprintKey, null);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        var fingerprint = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(FingerprintKey, fingerprint);
        return fingerprint;
    }
}
