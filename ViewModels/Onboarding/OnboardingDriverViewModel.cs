using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingDriverViewModel : BaseViewModel
{
    private readonly IOnboardingApiClient onboardingApiClient;
    private readonly IUserSessionService userSessionService;

    [ObservableProperty]
    private string? carMake;

    [ObservableProperty]
    private string? carModel;

    [ObservableProperty]
    private string? carYear;

    [ObservableProperty]
    private string? licensePlate;

    [ObservableProperty]
    private string? driverLicenseNumber;

    [ObservableProperty]
    private bool isAvailable = true;

    [ObservableProperty]
    private string? driverLicenseStatus = "No file selected";

    [ObservableProperty]
    private string? fullName;

    [ObservableProperty]
    private string? phoneNumber;

    [ObservableProperty]
    private string? driverPhotoPath;

    [ObservableProperty]
    private string? driverPhotoStatus = "No photo selected";

    public OnboardingDriverViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Vehicle Details";
        _ = InitializeProfileAsync();
    }

    [RelayCommand]
    public async Task UploadLicenseAsync()
    {
        DriverLicenseStatus = "License uploaded (placeholder)";
        await Shell.Current.DisplayAlertAsync("Upload", "Driver's license upload will be available soon.", "OK");
    }

    [RelayCommand]
    public async Task TakeDriverPhotoAsync()
    {
        await PickDriverPhotoAsync(async () => await MediaPicker.Default.CapturePhotoAsync(), "camera");
    }

    [RelayCommand]
    public async Task PickDriverPhotoAsync()
    {
        await PickDriverPhotoAsync(async () => await MediaPicker.Default.PickPhotoAsync(), "gallery");
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
            await Shell.Current.DisplayAlertAsync("Validation", "Please fill in all required fields.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var carDetails = new CarDetails
            {
                DriverFullName = FullName,
                DriverPhoneNumber = PhoneNumber,
                DriverPhotoPath = DriverPhotoPath,
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
            await userSessionService.ClearSubscriptionStateAsync();

            await Shell.Current.GoToAsync("//DriverHome");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InitializeProfileAsync()
    {
        var state = await userSessionService.GetStateAsync();

        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullName = state.FullName;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneNumber = state.PhoneNumber;
        }
    }

    private async Task PickDriverPhotoAsync(Func<Task<FileResult?>> photoPicker, string sourceLabel)
    {
        try
        {
            var photo = await photoPicker();
            if (photo is null)
            {
                return;
            }

            var savedPath = await SavePhotoAsync(photo);
            DriverPhotoPath = savedPath;
            DriverPhotoStatus = $"Photo selected from {sourceLabel}";
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to select driver photo");
            await Shell.Current.DisplayAlertAsync("Photo", "Unable to select a driver photo right now.", "OK");
        }
    }

    private static async Task<string> SavePhotoAsync(FileResult photo)
    {
        var safeFileName = $"driver-photo-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(photo.FileName)}";
        var targetPath = Path.Combine(FileSystem.CacheDirectory, safeFileName);

        await using var sourceStream = await photo.OpenReadAsync();
        await using var targetStream = File.OpenWrite(targetPath);
        await sourceStream.CopyToAsync(targetStream);

        return targetPath;
    }
}
