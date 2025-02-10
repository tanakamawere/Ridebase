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

    public async Task<ApiResponse<string>> CancelRide()
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<string>> GetRideDetails()
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<string>> GetRideStatus()
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest)
    {
        return await apiClient.PostAsync<RideRequestResponseModel>("api/rides/request", rideRequest);
    }

    public async Task<ApiResponse<string>> TrackRide()
    {
        throw new NotImplementedException();
    }
}
