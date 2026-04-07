using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Ridebase.Models;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingProfileViewModel : BaseViewModel
{
    private const string PendingFullNameKey = "onboarding_pending_full_name";
    private const string PendingPhoneNumberKey = "onboarding_pending_phone_number";
    private const string PendingCityKey = "onboarding_pending_city";

    [ObservableProperty]
    private string fullName;

    [ObservableProperty]
    private string phoneNumber;

    [ObservableProperty]
    private string selectedCity;

    [ObservableProperty]
    private bool locationPermissionGranted;

    partial void OnLocationPermissionGrantedChanged(bool value)
    {
        if (value)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
                
                if (status != PermissionStatus.Granted)
                {
                    // If they denied it, toggle the checkbox back off
                    LocationPermissionGranted = false;
                    await Shell.Current.DisplayAlertAsync("Permission Required", 
                        "Location access is needed to match you with nearby drivers.", "OK");
                }
            });
        }
    }

    [ObservableProperty]
    private bool profileConfirmed;

    public List<string> ZimbabweCities { get; } =
    [
        "Harare", "Bulawayo"
    ];

    public OnboardingProfileViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Complete Your Profile";
        _ = InitializeAsync();
    }

    [RelayCommand]
    public async Task ContinueAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(PhoneNumber) ||
            string.IsNullOrWhiteSpace(SelectedCity) ||
            !LocationPermissionGranted ||
            !ProfileConfirmed)
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please complete all fields and confirmations.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            await SecureStorage.SetAsync(PendingFullNameKey, FullName);
            await SecureStorage.SetAsync(PendingPhoneNumberKey, PhoneNumber);
            await SecureStorage.SetAsync(PendingCityKey, SelectedCity);
            await userSessionService.SetProfileAsync(FullName, PhoneNumber);
            await Shell.Current.GoToAsync(nameof(OnboardingRolePage));
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to store onboarding profile draft");
            await Shell.Current.DisplayAlertAsync("Error", "Unable to continue onboarding right now. Please try again.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InitializeAsync()
    {
        var state = await userSessionService.GetStateAsync();
        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullName = await SecureStorage.GetAsync(PendingFullNameKey) ?? state.FullName;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneNumber = await SecureStorage.GetAsync(PendingPhoneNumberKey) ?? state.PhoneNumber;
        }

        if (string.IsNullOrWhiteSpace(SelectedCity))
        {
            SelectedCity = await SecureStorage.GetAsync(PendingCityKey) ?? string.Empty;
        }
    }
}
