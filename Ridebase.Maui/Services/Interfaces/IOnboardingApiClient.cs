using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IOnboardingApiClient
{
    Task<ApiResponse<OnboardingProfileResponse>> GetCurrentProfileAsync();
    Task<ApiResponse<bool>> CheckOnboardingStatusAsync(string userId);
    Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile, AppUserRole role);
    Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails, string licensePhotoPath);
    Task<ApiResponse<string>> VerifyEmailOtpAsync(string code);
    Task<ApiResponse<string>> ResendOtpAsync();
}
