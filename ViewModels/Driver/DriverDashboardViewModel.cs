using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models.Ride;
using Ridebase.Pages.Driver;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public partial class DriverDashboardViewModel : BaseViewModel
{

    [ObservableProperty]
    private ObservableCollection<RideRequestModel> rideRequests;

    [ObservableProperty]
    private bool isOnline = false;
    [ObservableProperty]
    private string onlineStatusText = "You are offline";

    [ObservableProperty]
    private bool canGoOnline = true;

    [ObservableProperty]
    private string subscriptionMessage = string.Empty;

    public DriverDashboardViewModel(ILogger<DriverDashboardViewModel> logger, IRideRealtimeService _rideRealtimeService, IUserSessionService _userSessionService, IRideStateStore _rideStateStore)
    {
        Logger = logger;
        rideRealtimeService = _rideRealtimeService;
        userSessionService = _userSessionService;
        rideStateStore = _rideStateStore;
        RideRequests = [];

        rideRealtimeService.DriverRideRequestReceived += OnDriverRideRequestReceived;
        InitializeSubscriptionState();
    }

    partial void OnIsOnlineChanged(bool oldValue, bool newValue)
    {
        Logger.LogInformation("Driver online status changed from {OldValue} to {NewValue}", oldValue, newValue);
        if (newValue && !CanGoOnline)
        {
            IsOnline = false;
            return;
        }

        OnlineStatusText = newValue ? "Currently Online" : "You are offline";

        if (newValue)
        {
            _ = GoOnlineAsync();
        }
        else
        {
            _ = rideRealtimeService.StopAsync();
        }
    }

    private async Task GoOnlineAsync()
    {
        // Use the real persisted user ID, not a fresh random GUID
        var state = await userSessionService.GetStateAsync();
        var driverId = string.IsNullOrWhiteSpace(state.UserId)
            ? Guid.NewGuid().ToString("N")
            : state.UserId;
        await rideRealtimeService.StartDriverRequestStreamAsync(driverId);
    }

    //Method to go to ride in progress page
    [RelayCommand]
    public async Task GoToRideInProgress(RideRequestModel? request)
    {
        if (request is null)
        {
            return;
        }

        Logger.LogInformation("Navigating to Ride In Progress page");
        try
        {
            var session = await userSessionService.GetStateAsync();
            rideStateStore.SetCurrentRide(new RideSessionModel
            {
                RideId = request.RideGuid.ToString("N"),
                RiderId = request.RiderId,
                DriverId = Guid.TryParse(session.UserId, out var drvId) ? drvId : Guid.NewGuid(),
                RiderPhoneNumber = string.Empty, // populated by backend in production; unknown in mock
                DriverPhoneNumber = session.PhoneNumber,
                RiderName = "Rider",
                DriverName = session.FullName,
                VehicleInfo = "Driver vehicle",
                StartLocation = request.StartLocation,
                DestinationLocation = request.DestinationLocation,
                RiderOfferAmount = request.OfferAmount,
                AcceptedAmount = request.OfferAmount,
                DistanceKm = 5,
                EstimatedMinutes = 12,
                Status = RideStatus.DriverEnRoute
            });

            await rideRealtimeService.UpdateRideStatusAsync(request.RideGuid.ToString("N"), RideStatus.DriverEnRoute);
            await Shell.Current.GoToAsync(nameof(DriverRideProgressPage), true, new Dictionary<string, object>
            {
                {"currentLocation", "Pickup" }
            });
            Logger.LogInformation("Successfully navigated to Ride In Progress page");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to Ride In Progress page");
        }
    }

    private void OnDriverRideRequestReceived(DriverRideRequest request)
    {
        App.Current?.Dispatcher.Dispatch(() =>
        {
            RideRequests.Add(new RideRequestModel
            {
                RideGuid = request.RideId,
                RiderId = request.RiderId.ToString(),
                StartLocation = request.StartLocation,
                DestinationLocation = request.DestinationLocation,
                OfferAmount = request.OfferAmount,
                Comments = string.Empty
            });
        });
    }

    private async void InitializeSubscriptionState()
    {
        var state = await userSessionService.GetStateAsync();
        CanGoOnline = state.IsDriverSubscribed;
        SubscriptionMessage = CanGoOnline
            ? "Subscription active"
            : "Subscription required to receive ride requests.";
    }
}
