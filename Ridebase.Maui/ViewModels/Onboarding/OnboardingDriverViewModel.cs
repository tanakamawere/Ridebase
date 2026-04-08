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
    private string? driverLicensePhotoPath;

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
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select driver's license photo or PDF"
            });

            if (file is null)
            {
                return;
            }

            DriverLicensePhotoPath = file.FullPath;
            DriverLicenseStatus = $"Selected: {file.FileName}";
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to pick driver's license file");
            await Shell.Current.DisplayAlertAsync("Upload", "Unable to select a license file right now.", "OK");
        }
    }

    [RelayCommand]
    public async Task TakeDriverPhotoAsync()
    {
        await PickDriverPhotoAsync(async () => await MediaPicker.Default.CapturePhotoAsync(), "camera");
    }

    [RelayCommand]
    public async Task PickDriverPhotoAsync()
    {
        await PickDriverPhotoAsync(async () => 
        {
            var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 });
            return results?.FirstOrDefault();
        }, "gallery");
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
            string.IsNullOrWhiteSpace(DriverLicenseNumber) ||
            string.IsNullOrWhiteSpace(DriverLicensePhotoPath))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please fill in all required fields and upload your driver's license.", "OK");
            return;
        }

        if (!int.TryParse(CarYear, out var parsedYear) || parsedYear < 1900)
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please enter a valid vehicle year.", "OK");
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
                Year = parsedYear.ToString(),
                LicensePlate = LicensePlate,
                DriverLicenseNumber = DriverLicenseNumber,
                IsAvailable = IsAvailable
            };

            var submitResponse = await onboardingApiClient.SubmitDriverDetailsAsync(carDetails, DriverLicensePhotoPath);
            if (!submitResponse.IsSuccess)
            {
                var error = string.IsNullOrWhiteSpace(submitResponse.ErrorMessage)
                    ? "Unable to submit driver setup right now."
                    : submitResponse.ErrorMessage;
                await Shell.Current.DisplayAlertAsync("Driver setup", error, "OK");
                return;
            }

            await userSessionService.SetProfileAsync(FullName, PhoneNumber);
            await userSessionService.SetRoleAsync(AppUserRole.Driver);
            await userSessionService.SetOnboardedAsync(true);
            await userSessionService.ClearSubscriptionStateAsync();

            await Shell.Current.GoToAsync("//DriverHome");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to complete driver onboarding");
            await Shell.Current.DisplayAlertAsync("Driver setup", "Unable to complete driver setup right now. Please try again.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InitializeProfileAsync()
    {
        var state = await userSessionService.GetStateAsync();
        var profileResponse = await onboardingApiClient.GetCurrentProfileAsync();

        if (profileResponse.IsSuccess && profileResponse.Data is not null)
        {
            FullName = profileResponse.Data.FullName;
            PhoneNumber = profileResponse.Data.PhoneNumber;
            await userSessionService.SetProfileAsync(profileResponse.Data.FullName, profileResponse.Data.PhoneNumber);
            return;
        }

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
