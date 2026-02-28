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

    public List<string> ZimbabweCities { get; } =
    [
        "Harare", "Bulawayo", "Mutare", "Gweru", "Kwekwe", "Kadoma", "Masvingo",
        "Chinhoyi", "Norton", "Marondera", "Chegutu", "Zvishavane", "Bindura",
        "Beitbridge", "Hwange", "Victoria Falls", "Karoi", "Kariba", "Rusape", "Chiredzi"
    ];

    public OnboardingProfileViewModel(IOnboardingApiClient _onboardingApiClient)
    {
        onboardingApiClient = _onboardingApiClient;
        Title = "Complete Your Profile";
    }

    [RelayCommand]
    public async Task ContinueAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(PhoneNumber) ||
            string.IsNullOrWhiteSpace(SelectedCity))
        {
            await Shell.Current.DisplayAlert("Validation", "Please fill in all fields.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var profile = new OnboardingProfile
            {
                FullName = FullName,
                PhoneNumber = PhoneNumber,
                City = SelectedCity
            };

            await onboardingApiClient.SubmitProfileAsync(profile);
            await Shell.Current.GoToAsync(nameof(OnboardingRolePage));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
