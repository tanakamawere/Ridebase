using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingDriverViewModel : BaseViewModel
{
    [ObservableProperty]
    private string fullName;

    [ObservableProperty]
    private string phoneNumber;

    [ObservableProperty]
    private string carMake;

    [ObservableProperty]
    private string carModel;

    [ObservableProperty]
    private string carYear;

    [ObservableProperty]
    private string licensePlate;

    [ObservableProperty]
    private string driverLicenseNumber;

    [ObservableProperty]
    private bool isAvailable = true;

    [ObservableProperty]
    private bool hasActiveSubscription = true;

    [ObservableProperty]
    private string driverLicenseStatus = "No file selected";

    public OnboardingDriverViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Vehicle Details";
    }

    [RelayCommand]
    public async Task UploadLicenseAsync()
    {
        DriverLicenseStatus = "License uploaded (placeholder)";
        await Shell.Current.DisplayAlert("Upload", "Driver's license upload will be available soon.", "OK");
    }

    [RelayCommand]
    public async Task CompleteOnboardingAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(PhoneNumber) ||
            string.IsNullOrWhiteSpace(CarMake) ||
            string.IsNullOrWhiteSpace(CarModel) ||
            string.IsNullOrWhiteSpace(CarYear) ||
            string.IsNullOrWhiteSpace(LicensePlate) ||
            string.IsNullOrWhiteSpace(DriverLicenseNumber))
        {
            await Shell.Current.DisplayAlert("Validation", "Please fill in all required fields.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var carDetails = new CarDetails
            {
                DriverFullName = FullName,
                DriverPhoneNumber = PhoneNumber,
                Make = CarMake,
                Model = CarModel,
                Year = CarYear,
                LicensePlate = LicensePlate,
                DriverLicenseNumber = DriverLicenseNumber,
                IsAvailable = IsAvailable
            };

            await onboardingApiClient.SubmitDriverDetailsAsync(carDetails);
            await userSessionService.SetProfileAsync(FullName, PhoneNumber);
            await userSessionService.SetRoleAsync(AppUserRole.Driver);
            await userSessionService.SetOnboardedAsync(true);
            await userSessionService.SetDriverSubscriptionAsync(HasActiveSubscription);

            await Shell.Current.GoToAsync("//DriverHome");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
