using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Driver;

public partial class DriverSosViewModel : BaseViewModel
{
    [ObservableProperty]
    private string riderName = "Rider";

    [ObservableProperty]
    private string rideSummary = "Trip in progress";

    [ObservableProperty]
    private string selectedReasonCode = "unsafe_situation";

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private bool isSubmitting;

    public DriverSosViewModel(
        ILogger<DriverSosViewModel> logger,
        IRideStateStore rideStateStore,
        IRideApiClient rideApiClient)
    {
        Logger = logger;
        this.rideStateStore = rideStateStore;
        this.rideApiClient = rideApiClient;
        Title = "Driver SOS";

        if (rideStateStore.CurrentRide is not null)
        {
            SyncFromRide(rideStateStore.CurrentRide);
        }
    }

    [RelayCommand]
    private void SelectReason(string reasonCode)
    {
        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            SelectedReasonCode = reasonCode;
        }
    }

    [RelayCommand]
    private async Task SubmitSos()
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null || IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;
        try
        {
            var response = await rideApiClient.SubmitDriverSos(new DriverSosRequest
            {
                RideId = ride.RideId,
                DriverId = ride.DriverId,
                DriverName = ride.DriverName,
                RiderId = ride.RiderId,
                RiderName = ride.RiderName,
                TripStatus = ride.Status,
                ReasonCode = SelectedReasonCode,
                Message = string.IsNullOrWhiteSpace(Notes)
                    ? "Driver triggered SOS from the active trip screen."
                    : Notes.Trim(),
                CurrentLocation = ride.DriverCurrentLocation,
                TriggeredAtUtc = DateTimeOffset.UtcNow
            });

            if (response.IsSuccess)
            {
                await Shell.Current.DisplayAlert("SOS sent", "The emergency alert has been submitted to backend support placeholders.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("SOS failed", response.ErrorMessage ?? "We couldn't submit the SOS alert.", "OK");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to submit driver SOS");
            await Shell.Current.DisplayAlert("SOS failed", "We couldn't submit the SOS alert.", "OK");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void SyncFromRide(RideSessionModel ride)
    {
        RiderName = string.IsNullOrWhiteSpace(ride.RiderName) ? "Rider" : ride.RiderName;
        RideSummary = $"{ride.StartAddress} -> {ride.DestinationAddress}";
    }
}
