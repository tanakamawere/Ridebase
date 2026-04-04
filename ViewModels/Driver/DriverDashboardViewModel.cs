using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages.Driver;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public partial class DriverDashboardViewModel : BaseViewModel
{
    private readonly IPaymentSubscriptionApiClient paymentSubscriptionApiClient;

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

    [ObservableProperty]
    private string subscriptionDetail = string.Empty;

    [ObservableProperty]
    private bool isRefreshingSubscription;

    public DriverDashboardViewModel(
        ILogger<DriverDashboardViewModel> logger,
        IRideRealtimeService _rideRealtimeService,
        IUserSessionService _userSessionService,
        IPaymentSubscriptionApiClient _paymentSubscriptionApiClient,
        IRideStateStore _rideStateStore)
    {
        Logger = logger;
        rideRealtimeService = _rideRealtimeService;
        userSessionService = _userSessionService;
        paymentSubscriptionApiClient = _paymentSubscriptionApiClient;
        rideStateStore = _rideStateStore;
        RideRequests = [];

        rideRealtimeService.DriverRideRequestReceived += OnDriverRideRequestReceived;
        _ = RefreshSubscriptionStateAsync();
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
        await RefreshSubscriptionStateAsync();

        if (!CanGoOnline)
        {
            IsOnline = false;
            await Shell.Current.DisplayAlert("Subscription required", "Complete your subscription before going online.", "OK");
            return;
        }

        // Use the real persisted user ID, not a fresh random GUID
        var state = await userSessionService.GetStateAsync();
        var driverId = string.IsNullOrWhiteSpace(state.UserId)
            ? Guid.NewGuid().ToString("N")
            : state.UserId;
        await rideRealtimeService.StartDriverRequestStreamAsync(driverId);
    }

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
                RiderPhoneNumber = string.Empty,
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
                { "currentLocation", "Pickup" }
            });
            Logger.LogInformation("Successfully navigated to Ride In Progress page");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to Ride In Progress page");
        }
    }

    [RelayCommand]
    public Task GoToDriverProfile()
    {
        return Shell.Current.GoToAsync(nameof(DriverProfilePage));
    }

    [RelayCommand]
    public async Task RefreshSubscriptionStateAsync()
    {
        if (IsRefreshingSubscription)
        {
            return;
        }

        IsRefreshingSubscription = true;
        try
        {
            var state = await userSessionService.GetStateAsync();
            var subscriptionResponse = await paymentSubscriptionApiClient.GetSubscriptionStatusAsync();

            if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
            {
                await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
                state = await userSessionService.GetStateAsync();
            }

            CanGoOnline = state.IsDriverSubscribed;
            SubscriptionMessage = CanGoOnline
                ? "Subscription active"
                : "Subscription required to receive ride requests.";
            SubscriptionDetail = BuildSubscriptionDetail(state);
        }
        finally
        {
            IsRefreshingSubscription = false;
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

    private static string BuildSubscriptionDetail(UserBootstrapState state)
    {
        if (!state.IsDriverSubscribed)
        {
            return "Open billing to subscribe or reactivate your driver access.";
        }

        if (state.SubscriptionCurrentPeriodEnd is null)
        {
            return string.IsNullOrWhiteSpace(state.SubscriptionStatus)
                ? "Your subscription is active."
                : $"Current status: {state.SubscriptionStatus}.";
        }

        var renewalDate = DateTimeOffset.FromUnixTimeSeconds(state.SubscriptionCurrentPeriodEnd.Value).ToLocalTime();
        var cancellationNote = state.SubscriptionCancelAtPeriodEnd == true
            ? "Cancellation is scheduled at period end."
            : "Auto-renewal is active.";

        return $"Renews on {renewalDate:dd MMM yyyy}. {cancellationNote}";
    }
}
