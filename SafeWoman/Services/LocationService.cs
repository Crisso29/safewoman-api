namespace SafeWoman.Services;

public class LocationService
{
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                return null;

            // Intenta primero la última ubicación conocida (respuesta inmediata).
            var last = await Geolocation.Default.GetLastKnownLocationAsync();
            if (last is not null && (DateTime.UtcNow - last.Timestamp.UtcDateTime).TotalMinutes < 5)
                return last;

            // Si no hay ubicación reciente, solicita una nueva con precisión media y timeout reducido.
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch (FeatureNotSupportedException)
        {
            return null;
        }
        catch (PermissionException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
