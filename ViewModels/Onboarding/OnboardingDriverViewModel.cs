using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingDriverViewModel : BaseViewModel
{
    [ObservableProperty]
    private string carMake;

    [ObservableProperty]
    private string carModel;

    [ObservableProperty]
    private string carYear;

    [ObservableProperty]
    private string licensePlate;

    [ObservableProperty]
    private string driverLicenseStatus = "No file selected";

    public OnboardingDriverViewModel(IOnboardingApiClient _onboardingApiClient)
    {
        onboardingApiClient = _onboardingApiClient;
        Title = "Vehicle Details";
    }

    [RelayCommand]
    public async Task UploadLicenseAsync()
    {
        // Placeholder: driver's license upload will be wired to a file/camera picker
        DriverLicenseStatus = "License uploaded (placeholder)";
        await Shell.Current.DisplayAlert("Upload", "Driver's license upload will be available soon.", "OK");
    }

    [RelayCommand]
    public async Task CompleteOnboardingAsync()
    {
        if (string.IsNullOrWhiteSpace(CarMake) ||
            string.IsNullOrWhiteSpace(CarModel) ||
            string.IsNullOrWhiteSpace(CarYear) ||
            string.IsNullOrWhiteSpace(LicensePlate))
        {
            await Shell.Current.DisplayAlert("Validation", "Please fill in all vehicle details.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var carDetails = new CarDetails
            {
                Make = CarMake,
                Model = CarModel,
                Year = CarYear,
                LicensePlate = LicensePlate
            };

            await onboardingApiClient.SubmitDriverDetailsAsync(carDetails);

            // Open the driver shell window and return to the main app
            Application.Current.OpenWindow(new Window(new DriverShell()));
            await Shell.Current.GoToAsync("//Home");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
