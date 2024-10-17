namespace Ridebase.Services.RideService;

public interface IRideService
{
    //Request ride
    Task RequestRide();
    //Cancel ride
    Task CancelRide();
    //Get ride details
    Task GetRideDetails();
    //Get Ride status
    Task GetRideStatus();
    //Track ride of a driver or user
    Task TrackRide();
}
