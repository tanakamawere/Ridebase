using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models.Ride;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Rider;

public partial class RideProgressViewModel : BaseViewModel
{
    [ObservableProperty]
    private string rideStatusText = "Waiting for driver";

    [ObservableProperty]
    private string otherPartyPhoneNumber = string.Empty;

    [ObservableProperty]
    private string driverName = "Driver";

    [ObservableProperty]
    private string vehicleInfo = "Vehicle";

    [ObservableProperty]
    private string destinationText = "Destination available";

    [ObservableProperty]
    private string pickupText = "Pickup available";

    [ObservableProperty]
    private string estimatedFareText = "$0.00";

    [ObservableProperty]
    private string estimatedArrivalText = "Arriving soon";

    [ObservableProperty]
    private string etaHeadline = "Driver is heading to pickup";

    [ObservableProperty]
    private string statusDetail = "We will keep this page in sync with driver updates.";

    [ObservableProperty]
    private bool isDriverTrackingVisible = true;

    [ObservableProperty]
    private bool isJourneySummaryVisible;

    [ObservableProperty]
    private bool canRateTrip;

    [ObservableProperty]
    private string tripActionText = "Trip Details";

    public RideProgressViewModel(IRideStateStore rideStateStore, IRideRealtimeService rideRealtimeService)
    {
        this.rideStateStore = rideStateStore;
        this.rideRealtimeService = rideRealtimeService;
        Title = "Ride Progress";

        rideRealtimeService.RideStatusUpdated += OnRideStatusUpdated;
        rideRealtimeService.DriverLocationUpdated += OnDriverLocationUpdated;
        rideStateStore.RideChanged += OnRideChanged;

        if (rideStateStore.CurrentRide is not null)
        {
            SyncFromRide(rideStateStore.CurrentRide);
        }
    }

    [RelayCommand]
    public async Task CancelRide()
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null)
        {
            return;
        }

        ride.Status = RideStatus.Cancelled;
        rideStateStore.SetCurrentRide(ride);
        await rideRealtimeService.UpdateRideStatusAsync(ride.RideId, RideStatus.Cancelled);
        await Shell.Current.GoToAsync("//Home");
    }

    [RelayCommand]
    public async Task OpenCompletion()
    {
        if (!CanRateTrip)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(RideEndedPage));
    }

    private void OnRideChanged(RideSessionModel? ride)
    {
        if (ride is not null)
        {
            SyncFromRide(ride);
        }
    }

    private async void OnRideStatusUpdated(RideStatusUpdateEvent statusUpdate)
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null || !string.Equals(ride.RideId, statusUpdate.RideId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ride.Status = statusUpdate.Status;
        if (statusUpdate.EtaMinutes > 0)
        {
            ride.DriverEtaMinutes = statusUpdate.EtaMinutes;
        }

        if (!string.IsNullOrWhiteSpace(statusUpdate.StatusMessage))
        {
            ride.DriverStatusNote = statusUpdate.StatusMessage;
        }

        if (statusUpdate.Status == RideStatus.TripCompleted)
        {
            ride.CompletedAtUtc = statusUpdate.UpdatedAt;
        }

        rideStateStore.SetCurrentRide(ride);

        if (statusUpdate.Status == RideStatus.TripCompleted)
        {
            await Shell.Current.GoToAsync(nameof(RideEndedPage));
        }
    }

    private void OnDriverLocationUpdated(DriverLocationUpdate update)
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null || !string.Equals(ride.RideId, update.RideId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ride.DriverCurrentLocation = update.CurrentLocation;
        ride.DriverEtaMinutes = update.EtaMinutes;
        rideStateStore.SetCurrentRide(ride);
    }

    private void SyncFromRide(RideSessionModel ride)
    {
        RideStatusText = ride.Status switch
        {
            RideStatus.DriverEnRoute => "Driver en route",
            RideStatus.DriverArrived => "Driver arrived",
            RideStatus.TripStarted => "Journey in progress",
            RideStatus.TripCompleted => "Trip completed",
            RideStatus.Cancelled => "Ride cancelled",
            _ => "Waiting for driver"
        };

        DriverName = string.IsNullOrWhiteSpace(ride.DriverName) ? "Driver" : ride.DriverName;
        VehicleInfo = string.IsNullOrWhiteSpace(ride.VehicleInfo) ? "Vehicle" : ride.VehicleInfo;
        OtherPartyPhoneNumber = ride.DriverPhoneNumber;
        DestinationText = string.IsNullOrWhiteSpace(ride.DestinationAddress) ? $"{ride.DestinationLocation.latitude:F3}, {ride.DestinationLocation.longitude:F3}" : ride.DestinationAddress;
        PickupText = string.IsNullOrWhiteSpace(ride.StartAddress) ? $"{ride.StartLocation.latitude:F3}, {ride.StartLocation.longitude:F3}" : ride.StartAddress;
        EstimatedFareText = $"${ride.AcceptedAmount:F2}";
        EstimatedArrivalText = ride.DriverEtaMinutes > 0 ? $"{ride.DriverEtaMinutes} min" : "Arriving soon";
        StatusDetail = string.IsNullOrWhiteSpace(ride.DriverStatusNote) ? "Your driver updates will appear here." : ride.DriverStatusNote;

        IsDriverTrackingVisible = ride.Status is RideStatus.DriverEnRoute or RideStatus.DriverArrived;
        IsJourneySummaryVisible = ride.Status is RideStatus.TripStarted or RideStatus.TripCompleted;
        CanRateTrip = ride.Status == RideStatus.TripCompleted;
        EtaHeadline = ride.Status switch
        {
            RideStatus.DriverArrived => "Your driver is at the pickup point",
            RideStatus.TripStarted => "Your journey is underway",
            RideStatus.TripCompleted => "Thanks for riding with Kinetic",
            _ => "Driver is heading to pickup"
        };
        TripActionText = CanRateTrip ? "Rate Driver" : "Trip Details";
    }
}
