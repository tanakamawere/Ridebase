using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Driver;

[QueryProperty("currentLocation", "currentLocation")]
public partial class DriverRideProgressViewModel : BaseViewModel
{
    [ObservableProperty]
    private string currentLocation = string.Empty;

    [ObservableProperty]
    private string riderPhoneNumber = string.Empty;

    [ObservableProperty]
    private string currentStatus = "Driver en route";

    public DriverRideProgressViewModel(ILogger<DriverRideProgressViewModel> logger, IRideStateStore _rideStateStore, IRideRealtimeService _rideRealtimeService)
    {
        Logger = logger;
        rideStateStore = _rideStateStore;
        rideRealtimeService = _rideRealtimeService;
        Logger.LogInformation("DriverRideProgressViewModel initialized");

        if (rideStateStore.CurrentRide is not null)
        {
            RiderPhoneNumber = rideStateStore.CurrentRide.RiderPhoneNumber;
            CurrentStatus = MapStatus(rideStateStore.CurrentRide.Status);
        }
    }

    [RelayCommand]
    public async Task MarkArrived()
    {
        await UpdateStatus(RideStatus.DriverArrived);
    }

    [RelayCommand]
    public async Task StartTrip()
    {
        await UpdateStatus(RideStatus.TripStarted);
    }

    [RelayCommand]
    public async Task CompleteTrip()
    {
        await UpdateStatus(RideStatus.TripCompleted);
        await Shell.Current.GoToAsync("//DriverHome");
    }

    private async Task UpdateStatus(RideStatus status)
    {
        var currentRide = rideStateStore.CurrentRide;
        if (currentRide is null)
        {
            return;
        }

        rideStateStore.UpdateStatus(status);
        CurrentStatus = MapStatus(status);
        await rideRealtimeService.UpdateRideStatusAsync(currentRide.RideId, status);
    }

    private static string MapStatus(RideStatus status)
    {
        return status switch
        {
            RideStatus.DriverEnRoute => "Driver en route",
            RideStatus.DriverArrived => "Driver arrived",
            RideStatus.TripStarted => "Trip started",
            RideStatus.TripCompleted => "Trip completed",
            _ => "Driver en route"
        };
    }
}
