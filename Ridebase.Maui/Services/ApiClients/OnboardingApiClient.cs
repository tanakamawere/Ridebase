using Ridebase.Models;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Ridebase.Services.ApiClients;

public class OnboardingApiClient : IOnboardingApiClient
{
    private readonly HttpClient httpClient;

    public OnboardingApiClient(HttpClient _httpClient)
    {
        httpClient = _httpClient;
    }

    public async Task<ApiResponse<OnboardingProfileResponse>> GetCurrentProfileAsync(string? accessToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/onboarding/me");
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ApiResponse<OnboardingProfileResponse>
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = "Profile not found"
            };
        }

        return await response.ToApiResponseAsync<OnboardingProfileResponse>();
    }

    public async Task<ApiResponse<bool>> CheckOnboardingStatusAsync(string userId)
    {
        var profileResponse = await GetCurrentProfileAsync();
        return new ApiResponse<bool>
        {
            IsSuccess = profileResponse.IsSuccess,
            StatusCode = profileResponse.StatusCode,
            Data = profileResponse.IsSuccess,
            ErrorMessage = profileResponse.ErrorMessage
        };
    }

    public async Task<ApiResponse<string>> SubmitProfileAsync(OnboardingProfile profile, AppUserRole role)
    {
        var form = new Dictionary<string, string>
        {
            ["full_name"] = profile.FullName,
            ["email"] = profile.Email,
            ["phone_number"] = profile.PhoneNumber,
            ["city"] = profile.City,
            ["role"] = role == AppUserRole.Driver ? "DRIVER" : "RIDER"
        };

        var response = await httpClient.PostAsync("api/v1/onboarding/profile", new FormUrlEncodedContent(form));
        return await response.ToStringApiResponseAsync("Profile submitted");
    }

    public async Task<ApiResponse<string>> SubmitDriverDetailsAsync(CarDetails carDetails, string licensePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(licensePhotoPath) || !File.Exists(licensePhotoPath))
        {
            return new ApiResponse<string>
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "License photo file is required."
            };
        }

        var yearValue = int.TryParse(carDetails.Year, out var parsedYear) ? parsedYear : 0;

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(carDetails.Make), "car_make");
        multipart.Add(new StringContent(carDetails.Model), "car_model");
        multipart.Add(new StringContent(yearValue.ToString()), "year");
        multipart.Add(new StringContent(carDetails.LicensePlate), "license_plate");
        multipart.Add(new StringContent(carDetails.DriverLicenseNumber), "driver_license_number");
        multipart.Add(new StringContent(carDetails.IsAvailable.ToString().ToLowerInvariant()), "is_available_now");

        await using var fileStream = File.OpenRead(licensePhotoPath);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(licensePhotoPath));
        multipart.Add(fileContent, "license_photo", Path.GetFileName(licensePhotoPath));

        var response = await httpClient.PostAsync("api/v1/onboarding/driver_setup", multipart);
        return await response.ToStringApiResponseAsync("Driver setup submitted");
    }

    public async Task<ApiResponse<EmailVerificationResponse>> VerifyEmailOtpAsync(string code)
    {
        var response = await httpClient.PostAsJsonAsync("api/v1/onboarding/verify-email", new EmailVerificationRequest { Code = code });
        return await response.ToApiResponseAsync<EmailVerificationResponse>();
    }

    public async Task<ApiResponse<string>> ResendOtpAsync()
    {
        var response = await httpClient.PostAsync("api/v1/onboarding/resend-otp", null);
        return await response.ToStringApiResponseAsync("OTP resent successfully");
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
