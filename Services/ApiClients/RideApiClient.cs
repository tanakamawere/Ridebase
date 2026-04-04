using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class RideApiClient : IRideApiClient
{
    private readonly IApiClient apiClient;
    public RideApiClient(IApiClient _apiClient)
    {
        apiClient = _apiClient;
    }

    public async Task<ApiResponse<string>> CancelRide(string rideId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<RideSessionModel>> GetRideDetails(string rideId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<RideStatus>> GetRideStatus(string rideId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest)
    {
        return await apiClient.PostAsync<RideRequestResponseModel>("api/rides/request", rideRequest);
    }

    public async Task<ApiResponse<Ridebase.Models.Location>> TrackRide(string rideId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<RideSessionModel>> SelectOffer(RideAcceptRequest acceptRequest)
    {
        return await apiClient.PostAsync<RideSessionModel>("api/rides/select-offer", acceptRequest);
    }

    public async Task<ApiResponse<string>> SubmitRating(RideRatingRequest ratingRequest)
    {
        return await apiClient.PostAsync<string>("api/rides/rating", ratingRequest);
    }
}
