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

    public async Task<ApiResponse<OnboardingProfileResponse>> GetCurrentProfileAsync()
    {
        var state = await userSessionService.GetStateAsync();

        return new ApiResponse<OnboardingProfileResponse>
        {
            IsSuccess = state.IsOnboarded,
            StatusCode = state.IsOnboarded ? 200 : 404,
            Data = new OnboardingProfileResponse
            {
                FullName = state.FullName,
                PhoneNumber = state.PhoneNumber,
                City = string.Empty,
                Role = state.Role == AppUserRole.Driver ? "DRIVER" : "RIDER"
            },
            ErrorMessage = state.IsOnboarded ? string.Empty : "Profile not found"
        };
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

    public async Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile, AppUserRole role)
    {
        await userSessionService.SetProfileAsync(profile.FullName, profile.PhoneNumber);
        await userSessionService.SetRoleAsync(role);
        await userSessionService.SetOnboardedAsync(false);

        return new ApiResponse<string>
        {
            IsSuccess = true,
            Data = "Profile captured",
            StatusCode = 200
        };
    }

    public async Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails, string licensePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(licensePhotoPath))
        {
            return new ApiResponse<string>
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "License photo file is required."
            };
        }

        await userSessionService.SetOnboardedAsync(true);

        return new ApiResponse<string>
        {
            IsSuccess = true,
            Data = "Driver details captured",
            StatusCode = 200
        };
    }
}
