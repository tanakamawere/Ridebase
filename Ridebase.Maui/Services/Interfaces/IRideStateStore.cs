using Ridebase.Models.Ride;

namespace Ridebase.Services.Interfaces;

public interface IRideStateStore
{
    RideSessionModel? CurrentRide { get; }
    event Action<RideSessionModel?>? RideChanged;
    void SetCurrentRide(RideSessionModel? rideSession);
    void UpdateStatus(RideStatus status);
}
