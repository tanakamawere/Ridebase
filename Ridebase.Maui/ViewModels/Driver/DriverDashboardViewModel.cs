using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Ridebase.Models;
using Ridebase.Models.Driver;
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
    private bool isOnline;

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

    [ObservableProperty]
    private string driverDisplayName = "Kinetic Anchor";

    [ObservableProperty]
    private string currentPlanTitle = "Driver access pending";

    [ObservableProperty]
    private string currentPlanCaption = "CURRENT PLAN";

    [ObservableProperty]
    private string renewActionText = "Manage plan";

    [ObservableProperty]
    private decimal dailyGoalAmount = 32m;

    [ObservableProperty]
    private decimal dailyGoalTarget = 50m;

    [ObservableProperty]
    private string dailyGoalSupportText = "Keep going! You're building momentum.";

    [ObservableProperty]
    private string lastTripTitle = "Awaiting completed trips";

    [ObservableProperty]
    private string lastTripTimeText = "Your latest completed trip will appear here.";

    [ObservableProperty]
    private string lastTripEarningsText = "$0.00";

    [ObservableProperty]
    private string vehicleHeadline = "Vehicle details unavailable";

    [ObservableProperty]
    private string vehicleDiagnostics = "System diagnostics will appear here once connected.";

    [ObservableProperty]
    private ObservableCollection<DriverInsightModel> recentInsights;

    public DriverDashboardViewModel(
        ILogger<DriverDashboardViewModel> logger,
        IRideRealtimeService rideRealtimeService,
        IUserSessionService userSessionService,
        IPaymentSubscriptionApiClient paymentSubscriptionApiClient,
        IRideStateStore rideStateStore)
    {
        Logger = logger;
        this.rideRealtimeService = rideRealtimeService;
        this.userSessionService = userSessionService;
        this.paymentSubscriptionApiClient = paymentSubscriptionApiClient;
        this.rideStateStore = rideStateStore;

        RideRequests = [];
        RecentInsights =
        [
            new DriverInsightModel
            {
                Title = "Refuel Reminder",
                Detail = "Fuel reserves are trending low for the evening peak.",
                AgeText = "2H AGO",
                AccentColor = "#F6E3D7",
                IconGlyph = "\u26fd"
            },
            new DriverInsightModel
            {
                Title = "Peak Demand Alert",
                Detail = "CBD demand is rising. Staying online may improve your next fare.",
                AgeText = "LIVE",
                AccentColor = "#CCE8E7",
                IconGlyph = "\uf201"
            }
        ];

        this.rideRealtimeService.DriverRideRequestReceived += OnDriverRideRequestReceived;
        _ = InitializeDashboardAsync();
    }

    public double DailyGoalProgress => DailyGoalTarget <= 0 ? 0 : Math.Min(1, (double)(DailyGoalAmount / DailyGoalTarget));
    public string OnlineBadgeText => IsOnline ? "ONLINE" : "OFFLINE";

    partial void OnIsOnlineChanged(bool oldValue, bool newValue)
    {
        Logger.LogInformation("Driver online status changed from {OldValue} to {NewValue}", oldValue, newValue);
        if (newValue && !CanGoOnline)
        {
            IsOnline = false;
            return;
        }

        OnlineStatusText = newValue ? "Currently Online" : "You are offline";
        OnPropertyChanged(nameof(OnlineBadgeText));

        if (newValue)
        {
            _ = GoOnlineAsync();
        }
        else
        {
            _ = rideRealtimeService.StopAsync();
        }
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
            RideRequests.Remove(request);
            rideStateStore.SetCurrentRide(new RideSessionModel
            {
                RideId = request.RideGuid.ToString("N"),
                RiderId = request.RiderId,
                DriverId = Guid.TryParse(session.UserId, out var driverId) ? driverId : Guid.NewGuid(),
                RiderName = request.RiderName,
                RiderPhoneNumber = request.RiderPhoneNumber,
                DriverPhoneNumber = session.PhoneNumber,
                DriverName = session.FullName,
                VehicleInfo = "Driver vehicle",
                StartLocation = request.StartLocation,
                StartAddress = request.StartAddress,
                DestinationLocation = request.DestinationLocation,
                DestinationAddress = request.DestinationAddress,
                RiderOfferAmount = request.OfferAmount,
                RecommendedAmount = request.RecommendedAmount,
                AcceptedAmount = request.OfferAmount,
                DistanceKm = request.EstimatedDistanceKm <= 0 ? 5 : request.EstimatedDistanceKm,
                EstimatedMinutes = request.EstimatedMinutes <= 0 ? 12 : request.EstimatedMinutes,
                DriverEtaMinutes = 6,
                DriverCurrentLocation = request.StartLocation,
                AcceptedAtUtc = DateTimeOffset.UtcNow,
                Status = RideStatus.DriverEnRoute
            });

            LastTripTitle = FormatLocation(request.DestinationLocation);
            LastTripTimeText = $"Accepted from {FormatLocation(request.StartLocation)}";
            LastTripEarningsText = $"${request.OfferAmount:F2}";

            await rideRealtimeService.UpdateRideStatusAsync(request.RideGuid.ToString("N"), RideStatus.DriverEnRoute);
            await rideRealtimeService.PublishDriverLocationAsync(new DriverLocationUpdate
            {
                RideId = request.RideGuid.ToString("N"),
                DriverId = Guid.TryParse(session.UserId, out var activeDriverId) ? activeDriverId : Guid.NewGuid(),
                CurrentLocation = request.StartLocation,
                EtaMinutes = 6,
                DistanceToPickupKm = 2.3
            });
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
    public Task OpenGoogleMaps()
    {
        return Launcher.Default.OpenAsync("https://maps.google.com");
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
            DriverDisplayName = string.IsNullOrWhiteSpace(state.FullName) ? "Kinetic Anchor" : state.FullName;
            CurrentPlanTitle = BuildPlanTitle(state);
            RenewActionText = CanGoOnline ? "Renew Early" : "Subscribe Now";
            DailyGoalSupportText = CanGoOnline
                ? $"Only ${(DailyGoalTarget - DailyGoalAmount):0} left to hit your target."
                : "Complete subscription to unlock trip requests.";
            VehicleHeadline = string.IsNullOrWhiteSpace(state.FullName)
                ? "Vehicle setup in progress"
                : $"Vehicle assigned to {state.FullName}";
            VehicleDiagnostics = CanGoOnline
                ? "Dispatch systems synced and ready for the next request."
                : "Driver mode is paused until billing is active.";
        }
        finally
        {
            IsRefreshingSubscription = false;
        }
    }

    private async Task InitializeDashboardAsync()
    {
        var state = await userSessionService.GetStateAsync();
        DriverDisplayName = string.IsNullOrWhiteSpace(state.FullName) ? "Kinetic Anchor" : state.FullName;
        VehicleHeadline = string.IsNullOrWhiteSpace(state.FullName)
            ? "Vehicle setup in progress"
            : $"Ready vehicle for {state.FullName}";

        await RefreshSubscriptionStateAsync();
    }

    private async Task GoOnlineAsync()
    {
        await RefreshSubscriptionStateAsync();

        if (!CanGoOnline)
        {
            IsOnline = false;
            await Shell.Current.DisplayAlertAsync("Subscription required", "Complete your subscription before going online.", "OK");
            return;
        }

        var state = await userSessionService.GetStateAsync();
        var driverId = string.IsNullOrWhiteSpace(state.UserId)
            ? Guid.NewGuid().ToString("N")
            : state.UserId;
        await rideRealtimeService.StartDriverRequestStreamAsync(driverId);
    }

    private void OnDriverRideRequestReceived(DriverRideRequest request)
    {
        App.Current?.Dispatcher.Dispatch(() =>
        {
            var existing = RideRequests.FirstOrDefault(item => item.RideGuid == request.RideId);
            if (existing is not null)
            {
                RideRequests.Remove(existing);
            }

            RideRequests.Add(new RideRequestModel
            {
                RideGuid = request.RideId,
                RiderId = request.RiderId.ToString(),
                RiderName = request.RiderName,
                RiderPhoneNumber = request.RiderPhoneNumber,
                StartLocation = request.StartLocation,
                StartAddress = request.PickupAddress,
                DestinationLocation = request.DestinationLocation,
                DestinationAddress = request.DestinationAddress,
                OfferAmount = request.OfferAmount,
                RecommendedAmount = request.RecommendedAmount,
                EstimatedMinutes = request.EtaToPickupMinutes,
                EstimatedDistanceKm = (double)request.DistanceToPickupKm,
                Comments = $"Pickup {request.PickupAddress} • Drop-off {request.DestinationAddress}"
            });

            LastTripTitle = FormatLocation(request.DestinationLocation);
            LastTripTimeText = FormatLocation(request.StartLocation);
            LastTripEarningsText = $"${request.OfferAmount:F2}";
        });
    }

    [RelayCommand]
    public async Task CounterOffer(RideRequestModel? request)
    {
        if (request is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(DriverCounterOfferPage), true, new Dictionary<string, object>
        {
            { "rideRequest", request }
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

    private static string BuildPlanTitle(UserBootstrapState state)
    {
        if (!state.IsDriverSubscribed)
        {
            return "Driver access needs activation";
        }

        if (state.SubscriptionCurrentPeriodEnd is null)
        {
            return "Driver subscription is active";
        }

        var renewalDate = DateTimeOffset.FromUnixTimeSeconds(state.SubscriptionCurrentPeriodEnd.Value).ToLocalTime();
        var daysLeft = Math.Max(0, (renewalDate.Date - DateTime.Now.Date).Days);
        var suffix = daysLeft == 1 ? "day left" : "days left";
        return $"Weekly Elite - {daysLeft} {suffix}";
    }

    private static string FormatLocation(Ridebase.Models.Location? location)
    {
        if (location is null)
        {
            return "Location available";
        }

        return $"{location.latitude:F3}, {location.longitude:F3}";
    }
}
