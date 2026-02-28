using Ridebase.Models.Ride;

namespace Ridebase.Services.Interfaces;

public interface IRideRealtimeService
{
    event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    event Action<DriverRideRequest>? DriverRideRequestReceived;

    Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default);
    Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default);
    Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default);
    Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
