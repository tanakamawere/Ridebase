using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class MockDriverApiClient : IDriverApiClient
{
    private static readonly Ridebase.Models.Location HarareStart = new() { latitude = -17.8252, longitude = 31.0335 };
    private static readonly Ridebase.Models.Location HarareDest = new() { latitude = -17.8419, longitude = 31.0194 };

    /// <inheritdoc />
    public Task<DriverRideRequest> DriverRideRequestListener()
    {
        return Task.FromResult(BuildRequest(Guid.NewGuid()));
    }

    /// <inheritdoc />
    public Task<IEnumerable<DriverRideRequest>> GetPendingRideRequestsAsync(
        string driverId, CancellationToken cancellationToken = default)
    {
        var parsedId = Guid.TryParse(driverId, out var id) ? id : Guid.NewGuid();
        IEnumerable<DriverRideRequest> requests = [BuildRequest(parsedId)];
        return Task.FromResult(requests);
    }

    /// <inheritdoc />
    public Task<ApiResponse<bool>> AcceptRideRequestAsync(
        RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResponse<bool>
        {
            IsSuccess = true,
            Data = true,
            StatusCode = 200
        });
    }

    private static DriverRideRequest BuildRequest(Guid driverId) => new()
    {
        RideId = Guid.NewGuid(),
        DriverId = driverId,
        RiderId = Guid.NewGuid(),
        OfferAmount = 8.50m,
        Status = RideStatus.Requested,
        StartLocation = HarareStart,
        DestinationLocation = HarareDest
    };
}
