namespace Ridebase.Services.Interfaces;

public interface ILocationService
{
    Task<LocationAcquisitionResult> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
