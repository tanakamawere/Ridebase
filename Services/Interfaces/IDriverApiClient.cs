using Ridebase.Models.Ride;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IDriverApiClient
{
    /// <summary>Legacy single-request listener (mock path only).</summary>
    Task<DriverRideRequest> DriverRideRequestListener();

    /// <summary>Returns all pending ride requests within range for <paramref name="driverId"/>.</summary>
    Task<IEnumerable<DriverRideRequest>> GetPendingRideRequestsAsync(
        string driverId, CancellationToken cancellationToken = default);

    /// <summary>Accepts a specific ride request on behalf of the driver.</summary>
    Task<ApiResponse<bool>> AcceptRideRequestAsync(
        RideAcceptRequest request, CancellationToken cancellationToken = default);
}
