using Ridebase.Models;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class OnboardingApiClient : IOnboardingApiClient
{
    private readonly IApiClient apiClient;

    public OnboardingApiClient(IApiClient _apiClient)
    {
        apiClient = _apiClient;
    }

    public async Task<ApiResponse<bool>> CheckOnboardingStatusAsync(string userId)
    {
        return await apiClient.GetAsync<bool>($"user/{userId}/onboarding-status");
    }

    public async Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile)
    {
        return await apiClient.PostAsync<string>("user/onboarding-profile", profile);
    }

    public async Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails)
    {
        return await apiClient.PostAsync<string>("user/driver-details", carDetails);
    }
}
