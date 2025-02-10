using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IRideApiClient
{
    //Request ride
    Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest);
    //Cancel ride
    Task<ApiResponse<string>> CancelRide();
    //Get ride details
    Task<ApiResponse<string>> GetRideDetails();
    //Get Ride status
    Task<ApiResponse<string>> GetRideStatus();
    //Track ride of a driver or user
    Task<ApiResponse<string>> TrackRide();
}
