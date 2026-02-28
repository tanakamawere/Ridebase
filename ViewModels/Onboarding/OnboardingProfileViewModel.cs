using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingProfileViewModel : BaseViewModel
{
    [ObservableProperty]
    private string fullName;

    [ObservableProperty]
    private string phoneNumber;

    [ObservableProperty]
    private string selectedCity;

    [ObservableProperty]
    private bool locationPermissionGranted;

    [ObservableProperty]
    private bool profileConfirmed;

    public List<string> ZimbabweCities { get; } =
    [
        "Harare", "Bulawayo", "Mutare", "Gweru", "Kwekwe", "Kadoma", "Masvingo",
        "Chinhoyi", "Norton", "Marondera", "Chegutu", "Zvishavane", "Bindura",
        "Beitbridge", "Hwange", "Victoria Falls", "Karoi", "Kariba", "Rusape", "Chiredzi"
    ];

    public OnboardingProfileViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Complete Your Profile";
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
            await Shell.Current.DisplayAlert("Validation", "Please complete all fields and confirmations.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var profile = new OnboardingProfile
            {
                FullName = FullName,
                PhoneNumber = PhoneNumber,
                City = SelectedCity,
                DefaultLocationPermissionGranted = LocationPermissionGranted,
                ProfileConfirmed = ProfileConfirmed
            };

            await onboardingApiClient.SubmitProfileAsync(profile);
            await userSessionService.SetProfileAsync(FullName, PhoneNumber);
            await Shell.Current.GoToAsync(nameof(OnboardingRolePage));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
