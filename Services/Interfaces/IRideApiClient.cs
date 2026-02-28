using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IRideApiClient
{
    //Request ride
    Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest);
    //Cancel ride
    Task<ApiResponse<string>> CancelRide(string rideId);
    //Get ride details
    Task<ApiResponse<RideSessionModel>> GetRideDetails(string rideId);
    //Get Ride status
    Task<ApiResponse<RideStatus>> GetRideStatus(string rideId);
    //Track ride of a driver or user
    Task<ApiResponse<Ridebase.Models.Location>> TrackRide(string rideId);
}
