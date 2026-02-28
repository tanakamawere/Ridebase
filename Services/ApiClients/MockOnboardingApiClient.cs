using Ridebase.Models;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class MockOnboardingApiClient : IOnboardingApiClient
{
    private readonly IUserSessionService userSessionService;

    public MockOnboardingApiClient(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
    }

    public async Task<ApiResponse<bool>> CheckOnboardingStatusAsync(string userId)
    {
        var state = await userSessionService.GetStateAsync();

        return new ApiResponse<bool>
        {
            IsSuccess = true,
            Data = state.IsOnboarded,
            StatusCode = 200
        };
    }

    public async Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile)
    {
        await userSessionService.SetProfileAsync(profile.FullName, profile.PhoneNumber);
        await userSessionService.SetOnboardedAsync(false);

        return new ApiResponse<string>
        {
            IsSuccess = true,
            Data = "Profile captured",
            StatusCode = 200
        };
    }

    public async Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails)
    {
        await userSessionService.SetOnboardedAsync(true);

        return new ApiResponse<string>
        {
            IsSuccess = true,
            Data = "Driver details captured",
            StatusCode = 200
        };
    }
}
