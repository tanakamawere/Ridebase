using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Ridebase.Models;
using Ridebase.Models.Driver;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public partial class DriverProfileViewModel : BaseViewModel
{
    private const string CheckoutSuccessUrl = "https://ridebase.tech/subscription/success";
    private const string CheckoutCancelUrl = "https://ridebase.tech/subscription/cancel";

    private readonly IPaymentSubscriptionApiClient paymentSubscriptionApiClient;
    private readonly IOnboardingApiClient onboardingApiClient;

    [ObservableProperty]
    private bool isSubscribed;

    [ObservableProperty]
    private string subscriptionStatusText = "Loading subscription status...";

    [ObservableProperty]
    private string subscriptionPeriodText = string.Empty;

    [ObservableProperty]
    private string subscriptionIdText = string.Empty;

    [ObservableProperty]
    private string driverName = "Kinetic Anchor";

    [ObservableProperty]
    private string memberSinceText = "Verified partner";

    [ObservableProperty]
    private string activeVehicleName = "Vehicle pending";

    [ObservableProperty]
    private string activeVehiclePlate = "Assign a primary vehicle";

    [ObservableProperty]
    private ObservableCollection<DriverShortcutModel> profileShortcuts;

    public DriverProfileViewModel(
        ILogger<DriverProfileViewModel> logger,
        IUserSessionService userSessionService,
        IPaymentSubscriptionApiClient paymentSubscriptionApiClient,
        IOnboardingApiClient onboardingApiClient)
    {
        Logger = logger;
        this.userSessionService = userSessionService;
        this.paymentSubscriptionApiClient = paymentSubscriptionApiClient;
        this.onboardingApiClient = onboardingApiClient;
        ProfileShortcuts =
        [
            new DriverShortcutModel
            {
                Title = "Vehicle Documents",
                Subtitle = "Insurance, registration, and permit checks",
                AccentColor = "#CCE8E7",
                IconGlyph = "\uf15c"
            },
            new DriverShortcutModel
            {
                Title = "Subscription Management",
                Subtitle = "Billing, renewals, and plan visibility",
                AccentColor = "#F6E3D7",
                IconGlyph = "\uf555"
            },
            new DriverShortcutModel
            {
                Title = "Safety Settings",
                Subtitle = "Emergency contacts and support readiness",
                AccentColor = "#E8EDEE",
                IconGlyph = "\uf505"
            },
            new DriverShortcutModel
            {
                Title = "Support",
                Subtitle = "24/7 dispatch help and FAQs",
                AccentColor = "#E8EDEE",
                IconGlyph = "\uf059"
            }
        ];

        _ = RefreshSubscriptionStateAsync();
    }

    [RelayCommand]
    public async Task GoToRiderPages()
    {
        Logger.LogInformation("Switching to Rider Pages");
        await userSessionService.SetRoleAsync(AppUserRole.Rider);
        await Shell.Current.GoToAsync("//Home");
    }

    [RelayCommand]
    public Task RefreshSubscription()
    {
        return RefreshSubscriptionStateAsync();
    }

    [RelayCommand]
    public async Task StartCheckoutAsync()
    {
        var response = await paymentSubscriptionApiClient.CreateCheckoutSessionAsync(new SubscriptionCheckoutRequest
        {
            SuccessUrl = CheckoutSuccessUrl,
            CancelUrl = CheckoutCancelUrl
        });

        if (!response.IsSuccess || response.Data?.CheckoutUrl is null)
        {
            await Shell.Current.DisplayAlertAsync("Subscription", "Unable to start checkout right now.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(response.Data.CheckoutUrl);
        await RefreshSubscriptionStateAsync();
    }

    [RelayCommand]
    public async Task OpenBillingPortalAsync()
    {
        var response = await paymentSubscriptionApiClient.CreatePortalSessionAsync();

        if (!response.IsSuccess || response.Data?.PortalUrl is null)
        {
            await Shell.Current.DisplayAlertAsync("Billing", "Unable to open the billing portal right now.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(response.Data.PortalUrl);
        await RefreshSubscriptionStateAsync();
    }

    [RelayCommand]
    public async Task CancelSubscriptionAsync()
    {
        var state = await userSessionService.GetStateAsync();
        if (string.IsNullOrWhiteSpace(state.SubscriptionId))
        {
            await Shell.Current.DisplayAlertAsync("Billing", "No active subscription was found.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Cancel subscription",
            "Cancel at the end of the current billing period?",
            "Cancel at period end",
            "Keep subscription");

        if (!confirm)
        {
            return;
        }

        var response = await paymentSubscriptionApiClient.CancelSubscriptionAsync(state.SubscriptionId, new SubscriptionCancelRequest
        {
            CancelAtPeriodEnd = true
        });

        if (!response.IsSuccess || response.Data is null)
        {
            await Shell.Current.DisplayAlertAsync("Billing", "Unable to cancel the subscription right now.", "OK");
            return;
        }

        await userSessionService.SetSubscriptionStateAsync(response.Data);
        await RefreshSubscriptionStateAsync();
    }

    private async Task RefreshSubscriptionStateAsync()
    {
        var onboardingProfile = await onboardingApiClient.GetCurrentProfileAsync();
        var state = await userSessionService.GetStateAsync();
        var subscriptionResponse = await paymentSubscriptionApiClient.GetSubscriptionStatusAsync();

        if (onboardingProfile.IsSuccess && onboardingProfile.Data is not null)
        {
            DriverName = onboardingProfile.Data.FullName;
            MemberSinceText = string.IsNullOrWhiteSpace(onboardingProfile.Data.PhoneNumber)
                ? "Verified partner"
                : $"Verified partner • {onboardingProfile.Data.PhoneNumber}";
            await userSessionService.SetProfileAsync(onboardingProfile.Data.FullName, onboardingProfile.Data.PhoneNumber);
        }

        if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
        {
            await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
            state = await userSessionService.GetStateAsync();
        }

        IsSubscribed = state.IsDriverSubscribed;
        if (string.IsNullOrWhiteSpace(DriverName))
        {
            DriverName = string.IsNullOrWhiteSpace(state.FullName) ? "Kinetic Anchor" : state.FullName;
        }

        if (string.IsNullOrWhiteSpace(MemberSinceText) || MemberSinceText == "Verified partner")
        {
            MemberSinceText = string.IsNullOrWhiteSpace(state.PhoneNumber)
                ? "Verified partner"
                : $"Verified partner • {state.PhoneNumber}";
        }
        ActiveVehicleName = state.IsDriverSubscribed ? "White Toyota Corolla" : "Complete driver setup";
        ActiveVehiclePlate = state.IsDriverSubscribed ? "ABX-9082" : "Billing and vehicle details pending";
        SubscriptionStatusText = IsSubscribed ? "Subscription active" : "Subscription inactive";
        SubscriptionIdText = string.IsNullOrWhiteSpace(state.SubscriptionId)
            ? "Subscription ID not available yet"
            : $"Subscription ID: {state.SubscriptionId}";

        if (state.SubscriptionCurrentPeriodEnd is null)
        {
            SubscriptionPeriodText = string.IsNullOrWhiteSpace(state.SubscriptionStatus)
                ? "Manage billing from this screen."
                : $"Status: {state.SubscriptionStatus}";
            return;
        }

        var renewalDate = DateTimeOffset.FromUnixTimeSeconds(state.SubscriptionCurrentPeriodEnd.Value).ToLocalTime();
        SubscriptionPeriodText = state.SubscriptionCancelAtPeriodEnd == true
            ? $"Access ends on {renewalDate:dd MMM yyyy}."
            : $"Renews on {renewalDate:dd MMM yyyy}.";
    }
}
