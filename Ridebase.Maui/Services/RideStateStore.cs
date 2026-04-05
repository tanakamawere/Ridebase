using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class RideStateStore : IRideStateStore
{
    public RideSessionModel? CurrentRide { get; private set; }

    public event Action<RideSessionModel?>? RideChanged;

    public void SetCurrentRide(RideSessionModel? rideSession)
    {
        CurrentRide = rideSession;
        RideChanged?.Invoke(CurrentRide);
    }

    public void UpdateStatus(RideStatus status)
    {
        if (CurrentRide is null)
        {
            return;
        }

        CurrentRide.Status = status;
        RideChanged?.Invoke(CurrentRide);
    }
}
