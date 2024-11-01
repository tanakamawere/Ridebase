using Maui.Apps.Framework.Services;
using Ridebase.Models;

namespace Ridebase.Services.RideService;

public class RideService : RestServiceBase, IRideService
{
    public RideService(IConnectivity connectivity) : base(connectivity)
    {
        SetBaseURL(Constants.RidebaseApiUrl);
    }

    public Task CancelRide()
    {
        throw new NotImplementedException();
    }

    public Task GetRideDetails()
    {
        throw new NotImplementedException();
    }

    public Task GetRideStatus()
    {
        throw new NotImplementedException();
    }

    public async Task<User> GetUserInfo()
    {
        AddHttpHeader("Authorization", await SecureStorage.GetAsync("auth_token"));

        var response = await GetAsync<User>("userinfo");

        return response;
    }

    public async Task<string> PostAccessToken(string accessToken)
    {
        AddHttpHeader("Authorization", $"Bearer {accessToken}");

        var response = await PostAsync("login_registration", "");

        return await response.Content.ReadAsStringAsync();
    }

    public Task RequestRide()
    {
        throw new NotImplementedException();
    }

    public Task TrackRide()
    {
        throw new NotImplementedException();
    }
}
