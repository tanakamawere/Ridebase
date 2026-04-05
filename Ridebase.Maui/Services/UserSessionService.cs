using Ridebase.Models;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class UserSessionService : IUserSessionService
{
    private const string RoleKey = "user_role";
    private const string IsOnboardedKey = "user_is_onboarded";
    private const string DriverSubscribedKey = "driver_subscription";
    private const string SubscriptionIdKey = "driver_subscription_id";
    private const string SubscriptionCustomerIdKey = "driver_subscription_customer_id";
    private const string SubscriptionStatusKey = "driver_subscription_status";
    private const string SubscriptionCurrentPeriodStartKey = "driver_subscription_period_start";
    private const string SubscriptionCurrentPeriodEndKey = "driver_subscription_period_end";
    private const string SubscriptionCancelAtPeriodEndKey = "driver_subscription_cancel_at_period_end";
    private const string FullNameKey = "profile_full_name";
    private const string PhoneNumberKey = "profile_phone_number";

    public async Task<UserBootstrapState> GetStateAsync()
    {
        var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
        var role = await SecureStorage.GetAsync(RoleKey) ?? AppUserRole.Rider.ToString();
        var onboarded = await SecureStorage.GetAsync(IsOnboardedKey) ?? bool.FalseString;
        var subscriptionStatus = await SecureStorage.GetAsync(SubscriptionStatusKey) ?? string.Empty;
        var subscribed = await SecureStorage.GetAsync(DriverSubscribedKey) ?? bool.FalseString;
        var subscriptionCurrentPeriodStart = await SecureStorage.GetAsync(SubscriptionCurrentPeriodStartKey);
        var subscriptionCurrentPeriodEnd = await SecureStorage.GetAsync(SubscriptionCurrentPeriodEndKey);
        var subscriptionCancelAtPeriodEnd = await SecureStorage.GetAsync(SubscriptionCancelAtPeriodEndKey);

        return new UserBootstrapState
        {
            UserId = userId,
            Role = Enum.TryParse<AppUserRole>(role, true, out var parsedRole) ? parsedRole : AppUserRole.Rider,
            IsOnboarded = bool.TryParse(onboarded, out var parsedOnboarded) && parsedOnboarded,
            IsDriverSubscribed = IsSubscribedStatus(subscriptionStatus) || (bool.TryParse(subscribed, out var parsedSubscribed) && parsedSubscribed),
            SubscriptionId = await SecureStorage.GetAsync(SubscriptionIdKey),
            CustomerId = await SecureStorage.GetAsync(SubscriptionCustomerIdKey),
            SubscriptionStatus = subscriptionStatus,
            SubscriptionCurrentPeriodStart = long.TryParse(subscriptionCurrentPeriodStart, out var parsedPeriodStart) ? parsedPeriodStart : null,
            SubscriptionCurrentPeriodEnd = long.TryParse(subscriptionCurrentPeriodEnd, out var parsedPeriodEnd) ? parsedPeriodEnd : null,
            SubscriptionCancelAtPeriodEnd = bool.TryParse(subscriptionCancelAtPeriodEnd, out var parsedCancelAtPeriodEnd) ? parsedCancelAtPeriodEnd : null,
            FullName = await SecureStorage.GetAsync(FullNameKey) ?? string.Empty,
            PhoneNumber = await SecureStorage.GetAsync(PhoneNumberKey) ?? string.Empty
        };
    }

    public async Task SetRoleAsync(AppUserRole role)
    {
        await SecureStorage.SetAsync(RoleKey, role.ToString());
    }

    public async Task SetOnboardedAsync(bool isOnboarded)
    {
        await SecureStorage.SetAsync(IsOnboardedKey, isOnboarded.ToString());
    }

    public async Task SetDriverSubscriptionAsync(bool isSubscribed)
    {
        await SecureStorage.SetAsync(DriverSubscribedKey, isSubscribed.ToString());
        await SecureStorage.SetAsync(SubscriptionStatusKey, isSubscribed ? "active" : "canceled");
    }

    public async Task SetSubscriptionStateAsync(DriverSubscriptionStatus subscriptionState)
    {
        await SecureStorage.SetAsync(DriverSubscribedKey, subscriptionState.IsSubscribed.ToString());
        await WriteValueOrRemoveAsync(SubscriptionIdKey, subscriptionState.SubscriptionId);
        await WriteValueOrRemoveAsync(SubscriptionCustomerIdKey, subscriptionState.CustomerId);
        await WriteValueOrRemoveAsync(SubscriptionStatusKey, subscriptionState.Status);
        await WriteValueOrRemoveAsync(SubscriptionCurrentPeriodStartKey, subscriptionState.CurrentPeriodStart?.ToString());
        await WriteValueOrRemoveAsync(SubscriptionCurrentPeriodEndKey, subscriptionState.CurrentPeriodEnd?.ToString());
        await WriteValueOrRemoveAsync(SubscriptionCancelAtPeriodEndKey, subscriptionState.CancelAtPeriodEnd?.ToString());
    }

    public Task ClearSubscriptionStateAsync()
    {
        SecureStorage.Remove(DriverSubscribedKey);
        SecureStorage.Remove(SubscriptionIdKey);
        SecureStorage.Remove(SubscriptionCustomerIdKey);
        SecureStorage.Remove(SubscriptionStatusKey);
        SecureStorage.Remove(SubscriptionCurrentPeriodStartKey);
        SecureStorage.Remove(SubscriptionCurrentPeriodEndKey);
        SecureStorage.Remove(SubscriptionCancelAtPeriodEndKey);
        return Task.CompletedTask;
    }

    public async Task SetProfileAsync(string fullName, string phoneNumber)
    {
        await SecureStorage.SetAsync(FullNameKey, fullName);
        await SecureStorage.SetAsync(PhoneNumberKey, phoneNumber);
    }

    public Task<User> BuildUserAsync(string userId, string accessToken, string displayName)
    {
        return Task.FromResult(new User
        {
            UserId = userId,
            UserName = displayName,
            AccessToken = accessToken
        });
    }

    private static bool IsSubscribedStatus(string status)
    {
        return status.Equals("active", StringComparison.OrdinalIgnoreCase)
            || status.Equals("trialing", StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteValueOrRemoveAsync(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SecureStorage.Remove(key);
            return Task.CompletedTask;
        }

        return SecureStorage.SetAsync(key, value);
    }
}
