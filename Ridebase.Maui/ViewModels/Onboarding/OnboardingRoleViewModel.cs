using Ridebase.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
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

            var submitResponse = await onboardingApiClient.SubmitProfileAsync(new OnboardingProfile
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                City = city,
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

            if (role == AppUserRole.Driver)
            {
                await userSessionService.SetOnboardedAsync(false);
                await Shell.Current.GoToAsync(nameof(OnboardingDriverPage));
            }
            else
            {
                await userSessionService.SetOnboardedAsync(true);
                await Shell.Current.GoToAsync("//Home");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
