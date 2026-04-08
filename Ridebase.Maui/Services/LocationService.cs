using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class LocationService : ILocationService
{
    private CancellationTokenSource? _cancelTokenSource;

    public async Task<LocationAcquisitionResult> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    return new LocationAcquisitionResult
                    {
                        Status = LocationAcquisitionStatus.PermissionDenied,
                        ErrorMessage = "Location permission was denied."
                    };
                }
            }

            // Try to get the last known location first (faster)
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            
            // On some emulators, last known can be (0,0) which is invalid for our use case (Zimbabwe)
            if (location != null && location.Latitude == 0 && location.Longitude == 0)
            {
                location = null;
            }

            if (location == null)
            {
                // If last known is not available or invalid, try to get current location
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                _cancelTokenSource = new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancelTokenSource.Token, cancellationToken);
                location = await Geolocation.Default.GetLocationAsync(request, linkedCts.Token);
            }

            if (location is not null)
            {
                return new LocationAcquisitionResult
                {
                    Status = LocationAcquisitionStatus.Success,
                    DeviceLocation = location
                };
            }
        }
        catch (FeatureNotSupportedException)
        {
            return new LocationAcquisitionResult
            {
                Status = LocationAcquisitionStatus.NotSupported,
                ErrorMessage = "Geolocation is not supported on this device."
            };
        }
        catch (PermissionException)
        {
            return new LocationAcquisitionResult
            {
                Status = LocationAcquisitionStatus.PermissionDenied,
                ErrorMessage = "Location permissions are not granted."
            };
        }
        catch (Exception ex)
        {
            return new LocationAcquisitionResult
            {
                Status = LocationAcquisitionStatus.Error,
                ErrorMessage = ex.Message
            };
        }

        return new LocationAcquisitionResult
        {
            Status = LocationAcquisitionStatus.Unavailable,
            ErrorMessage = "We couldn't determine the device location."
        };
    }
}
