using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.RideService;

public interface IRideService
{
    Task<ApiResponse<string>> PostAccessToken(string accessToken);
    Task<ApiResponse<User>> GetUserInfo();
    //Request ride
    Task<ApiResponse<RideRequestResponse>> RequestRide(RideRequest rideRequest);
    //Cancel ride
    Task<ApiResponse<string>> CancelRide();
    //Get ride details
    Task<ApiResponse<string>> GetRideDetails();
    //Get Ride status
    Task<ApiResponse<string>> GetRideStatus();
    //Track ride of a driver or user
    Task<ApiResponse<string>> TrackRide();
}
