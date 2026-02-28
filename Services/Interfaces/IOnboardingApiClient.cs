using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IOnboardingApiClient
{
    Task<ApiResponse<bool>> CheckOnboardingStatusAsync(string userId);
    Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile);
    Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails);
}
