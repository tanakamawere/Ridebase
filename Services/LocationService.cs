namespace Ridebase.Services;

public class LocationService
{
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;

    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            // Request location with high accuracy
            _isCheckingLocation = true;

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));

            _cancelTokenSource = new CancellationTokenSource();

            Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

            if (location != null)
                return location;
        }
        catch (FeatureNotSupportedException)
        {
            Console.WriteLine("Geolocation is not supported on this device.");
        }
        catch (PermissionException)
        {
            Console.WriteLine("Location permissions are not granted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to get location: {ex.Message}");
        }

        return null;
    }
}

public class LocationWithAddress
{
    public Models.Location Location { get; set; }
    public string FormattedAddress { get; set; }
}