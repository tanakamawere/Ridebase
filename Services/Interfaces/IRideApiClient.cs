using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IRideApiClient
{
    Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest);
    Task<ApiResponse<string>> CancelRide(string rideId);
    Task<ApiResponse<RideSessionModel>> GetRideDetails(string rideId);
    Task<ApiResponse<RideStatus>> GetRideStatus(string rideId);
    Task<ApiResponse<Ridebase.Models.Location>> TrackRide(string rideId);
    Task<ApiResponse<RideSessionModel>> SelectOffer(RideAcceptRequest acceptRequest);
    Task<ApiResponse<string>> SubmitRating(RideRatingRequest ratingRequest);
}
