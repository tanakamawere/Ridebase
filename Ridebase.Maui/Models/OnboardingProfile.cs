namespace Ridebase.Models;
using System.Text.Json.Serialization;

public class OnboardingProfile
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("default_location_permission_granted")]
    public bool DefaultLocationPermissionGranted { get; set; }

    [JsonPropertyName("profile_confirmed")]
    public bool ProfileConfirmed { get; set; }
}

public class OnboardingProfileResponse
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("email_otp_sent")]
    public bool EmailOtpSent { get; set; }
}

public class EmailVerificationRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public class EmailVerificationResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
