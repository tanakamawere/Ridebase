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
    private string riderName = "Rider";

    [ObservableProperty]
    private string currentStatus = "Driver en route";

    [ObservableProperty]
    private string pickupAddress = "Pickup";

    [ObservableProperty]
    private string destinationAddress = "Destination";

    [ObservableProperty]
    private string acceptedFareText = "$0.00";

    public DriverRideProgressViewModel(ILogger<DriverRideProgressViewModel> logger, IRideStateStore rideStateStore, IRideRealtimeService rideRealtimeService)
    {
        Logger = logger;
        this.rideStateStore = rideStateStore;
        this.rideRealtimeService = rideRealtimeService;

        if (rideStateStore.CurrentRide is not null)
        {
            SyncFromRide(rideStateStore.CurrentRide);
        }
    }

    [RelayCommand]
    public async Task MarkArrived()
    {
        await UpdateStatus(RideStatus.DriverArrived, "Driver has arrived at pickup.");
    }

    [RelayCommand]
    public async Task StartTrip()
    {
        await UpdateStatus(RideStatus.TripStarted, "Trip started.");
    }

    [RelayCommand]
    public async Task CompleteTrip()
    {
        var currentRide = rideStateStore.CurrentRide;
        if (currentRide is null)
        {
            return;
        }

        currentRide.Status = RideStatus.TripCompleted;
        currentRide.CompletedAtUtc = DateTimeOffset.UtcNow;
        rideStateStore.SetCurrentRide(currentRide);
        CurrentStatus = MapStatus(RideStatus.TripCompleted);

        await rideRealtimeService.CompleteRideAsync(currentRide.RideId);
        await Shell.Current.GoToAsync("//DriverHome");
    }

    private async Task UpdateStatus(RideStatus status, string statusMessage)
    {
        var currentRide = rideStateStore.CurrentRide;
        if (currentRide is null)
        {
            return;
        }

        currentRide.Status = status;
        currentRide.DriverStatusNote = statusMessage;
        rideStateStore.SetCurrentRide(currentRide);
        CurrentStatus = MapStatus(status);

        await rideRealtimeService.UpdateRideStatusAsync(currentRide.RideId, status);
        await rideRealtimeService.PublishDriverLocationAsync(new DriverLocationUpdate
        {
            RideId = currentRide.RideId,
            DriverId = currentRide.DriverId,
            CurrentLocation = status == RideStatus.TripStarted ? currentRide.DestinationLocation : currentRide.StartLocation,
            EtaMinutes = status == RideStatus.DriverArrived ? 0 : 4,
            DistanceToPickupKm = status == RideStatus.DriverArrived ? 0 : 1.2
        });
    }

    private void SyncFromRide(RideSessionModel ride)
    {
        RiderPhoneNumber = ride.RiderPhoneNumber;
        RiderName = ride.RiderName;
        PickupAddress = ride.StartAddress;
        DestinationAddress = ride.DestinationAddress;
        AcceptedFareText = $"${ride.AcceptedAmount:F2}";
        CurrentStatus = MapStatus(ride.Status);
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
