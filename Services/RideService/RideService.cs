using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.RideService;

public class RideService : IRideService
{
    private readonly IApiClient apiClient;
    public RideService(IApiClient _apiClient)
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

    public async Task<ApiResponse<RideRequestResponse>> RequestRide(RideRequest rideRequest)
    {
        return await apiClient.PostAsync<RideRequestResponse>("api/rides/request", rideRequest);
    }

    public async Task<ApiResponse<string>> TrackRide()
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<User>> GetUserInfo()
    {
        var headers = new Dictionary<string, string>
        {
            {"Authorization", await SecureStorage.GetAsync("auth_token") }
        };

        var response = await apiClient.GetAsync<User>("userinfo");

        return response;
    }

    public async Task<ApiResponse<string>> PostAccessToken(string accessToken)
    {
        var headers = new Dictionary<string, string> 
        {
            {"Authorization", $"Bearer {accessToken}" }
        };

        var response = await apiClient.PostAsync<string>("login_registration", "");

        return response;
    }
}
