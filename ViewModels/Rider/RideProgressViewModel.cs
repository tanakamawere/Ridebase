using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Rider;

public partial class RideProgressViewModel : BaseViewModel
{
    [ObservableProperty]
    private string rideStatusText = "Waiting for driver";

    [ObservableProperty]
    private string otherPartyPhoneNumber = string.Empty;

    [ObservableProperty]
    private bool canCompleteView;

    [ObservableProperty]
    private string driverName = "Driver";

    [ObservableProperty]
    private string vehicleInfo = "Vehicle";

    [ObservableProperty]
    private string destinationText = "Destination available";

    [ObservableProperty]
    private string estimatedFareText = "$0.00";

    [ObservableProperty]
    private string estimatedArrivalText = "Arriving soon";

    public RideProgressViewModel(IRideStateStore _rideStateStore, IRideRealtimeService _rideRealtimeService)
    {
        rideStateStore = _rideStateStore;
        rideRealtimeService = _rideRealtimeService;
        Title = "Ride Progress";

        rideRealtimeService.RideStatusUpdated += OnRideStatusUpdated;
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

        rideStateStore.UpdateStatus(RideStatus.Cancelled);
        await rideRealtimeService.UpdateRideStatusAsync(ride.RideId, RideStatus.Cancelled);
        await Shell.Current.GoToAsync("//Home");
    }

    private void OnRideChanged(RideSessionModel? ride)
    {
        if (ride is null)
        {
            return;
        }

        SyncFromRide(ride);
    }

    private void OnRideStatusUpdated(RideStatusUpdateEvent statusUpdate)
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null || !string.Equals(ride.RideId, statusUpdate.RideId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        rideStateStore.UpdateStatus(statusUpdate.Status);
    }

    private void SyncFromRide(RideSessionModel ride)
    {
        RideStatusText = ride.Status switch
        {
            RideStatus.DriverEnRoute => "Driver en route",
            RideStatus.DriverArrived => "Driver arrived",
            RideStatus.TripStarted => "Trip started",
            RideStatus.TripCompleted => "Trip completed",
            RideStatus.Cancelled => "Ride cancelled",
            _ => "Waiting for driver"
        };

        OtherPartyPhoneNumber = ride.DriverPhoneNumber;
        DriverName = string.IsNullOrWhiteSpace(ride.DriverName) ? "Driver" : ride.DriverName;
        VehicleInfo = string.IsNullOrWhiteSpace(ride.VehicleInfo) ? "Vehicle" : ride.VehicleInfo;
        DestinationText = $"{ride.DestinationLocation.latitude:F3}, {ride.DestinationLocation.longitude:F3}";
        EstimatedFareText = $"${ride.AcceptedAmount:F2}";
        EstimatedArrivalText = ride.EstimatedMinutes <= 0 ? "Arriving soon" : $"{ride.EstimatedMinutes} min";
        CanCompleteView = ride.Status is RideStatus.TripCompleted or RideStatus.Cancelled;
    }
}
