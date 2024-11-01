using Ridebase.Models;

namespace Ridebase.Services.RideService;

public interface IRideService
{
    Task<string> PostAccessToken(string accessToken);
    Task<User> GetUserInfo();
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
