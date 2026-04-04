using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Ridebase.Models;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Driver;

public partial class DriverProfileViewModel : BaseViewModel
{
    private const string CheckoutSuccessUrl = "https://ridebase.tech/subscription/success";
    private const string CheckoutCancelUrl = "https://ridebase.tech/subscription/cancel";

    private readonly IPaymentSubscriptionApiClient paymentSubscriptionApiClient;

    [ObservableProperty]
    private bool isSubscribed;

    [ObservableProperty]
    private string subscriptionStatusText = "Loading subscription status...";

    [ObservableProperty]
    private string subscriptionPeriodText = string.Empty;

    [ObservableProperty]
    private string subscriptionIdText = string.Empty;

    public DriverProfileViewModel(
        ILogger<DriverProfileViewModel> logger,
        IUserSessionService _userSessionService,
        IPaymentSubscriptionApiClient _paymentSubscriptionApiClient)
    {
        Logger = logger;
        userSessionService = _userSessionService;
        paymentSubscriptionApiClient = _paymentSubscriptionApiClient;
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
            await Shell.Current.DisplayAlert("Subscription", "Unable to start checkout right now.", "OK");
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
            await Shell.Current.DisplayAlert("Billing", "Unable to open the billing portal right now.", "OK");
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
            await Shell.Current.DisplayAlert("Billing", "No active subscription was found.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
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
            await Shell.Current.DisplayAlert("Billing", "Unable to cancel the subscription right now.", "OK");
            return;
        }

        await userSessionService.SetSubscriptionStateAsync(response.Data);
        await RefreshSubscriptionStateAsync();
    }

    private async Task RefreshSubscriptionStateAsync()
    {
        var state = await userSessionService.GetStateAsync();
        var subscriptionResponse = await paymentSubscriptionApiClient.GetSubscriptionStatusAsync();

        if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
        {
            await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
            state = await userSessionService.GetStateAsync();
        }

        IsSubscribed = state.IsDriverSubscribed;
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