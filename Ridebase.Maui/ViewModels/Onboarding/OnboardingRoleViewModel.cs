using Ridebase.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Ridebase.Pages.Auth;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingRoleViewModel : BaseViewModel
{
    private const string PendingFullNameKey = "onboarding_pending_full_name";
    private const string PendingPhoneNumberKey = "onboarding_pending_phone_number";
    private const string PendingCityKey = "onboarding_pending_city";

    public OnboardingRoleViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Choose Your Role";
    }

    [RelayCommand]
    public async Task ChooseDriverAsync()
    {
        await SubmitProfileAndContinueAsync(AppUserRole.Driver);
    }

    [RelayCommand]
    public async Task ChooseRiderAsync()
    {
        await SubmitProfileAndContinueAsync(AppUserRole.Rider);
    }

    private async Task SubmitProfileAndContinueAsync(AppUserRole role)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Wait for background login to persist the token (race with SignUpViewModel)
            var token = await SecureStorage.GetAsync("auth_token");
            for (int i = 0; i < 10 && string.IsNullOrEmpty(token); i++)
            {
                await Task.Delay(500);
                token = await SecureStorage.GetAsync("auth_token");
            }

            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.DisplayAlertAsync("Session expired", "Please sign in again.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var state = await userSessionService.GetStateAsync();
            var fullName = await SecureStorage.GetAsync(PendingFullNameKey) ?? state.FullName;
            var phoneNumber = await SecureStorage.GetAsync(PendingPhoneNumberKey) ?? state.PhoneNumber;
            var city = await SecureStorage.GetAsync(PendingCityKey) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(city))
            {
                await Shell.Current.DisplayAlertAsync("Profile required", "Please complete your profile details first.", "OK");
                await Shell.Current.GoToAsync("..", true);
                return;
            }

            var email = await SecureStorage.GetAsync("user_email") ?? string.Empty;

            var submitResponse = await onboardingApiClient.SubmitProfileAsync(new OnboardingProfile
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                City = city,
                Email = email,
                DefaultLocationPermissionGranted = true,
                ProfileConfirmed = true
            }, role);

            if (!submitResponse.IsSuccess)
            {
                var error = string.IsNullOrWhiteSpace(submitResponse.ErrorMessage)
                    ? "Unable to create onboarding profile."
                    : submitResponse.ErrorMessage;
                await Shell.Current.DisplayAlertAsync("Onboarding", error, "OK");
                return;
            }

            await userSessionService.SetProfileAsync(fullName, phoneNumber);
            await userSessionService.SetRoleAsync(role);
            SecureStorage.Remove(PendingFullNameKey);
            SecureStorage.Remove(PendingPhoneNumberKey);
            SecureStorage.Remove(PendingCityKey);

            // Profile creation triggers OTP email — navigate to verification
            var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
            var accessToken = await SecureStorage.GetAsync("auth_token") ?? string.Empty;
            var displayName = await SecureStorage.GetAsync("user_display_name") ?? fullName;
            var pictureUrl = await SecureStorage.GetAsync("user_image_url") ?? string.Empty;

            Console.WriteLine($"[ONBOARD-ROLE] accessToken: {accessToken}");
            Console.WriteLine($"[ONBOARD-ROLE] userId: {userId}, email: {email}, role: {role}");

            var navParams = new Dictionary<string, object>
            {
                ["UserEmail"] = email,
                ["UserId"] = userId,
                ["AccessToken"] = accessToken,
                ["DisplayName"] = displayName,
                ["PictureUrl"] = pictureUrl,
                ["SelectedRole"] = role == AppUserRole.Driver ? "DRIVER" : "RIDER"
            };
            await Shell.Current.GoToAsync(nameof(EmailVerificationPage), navParams);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
