using Ridebase.Models;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class UserSessionService : IUserSessionService
{
    private const string AuthTokenKey = "auth_token";
    private const string IdTokenKey = "id_token";
    private const string UserIdKey = "user_id";
    private const string RefreshTokenKey = "refresh_token";
    private const string RoleKey = "user_role";
    private const string IsOnboardedKey = "user_is_onboarded";
    private const string DriverSubscribedKey = "driver_subscription";
    private const string SubscriptionIdKey = "driver_subscription_id";
    private const string SubscriptionCustomerIdKey = "driver_subscription_customer_id";
    private const string SubscriptionStatusKey = "driver_subscription_status";
    private const string SubscriptionCurrentPeriodStartKey = "driver_subscription_period_start";
    private const string SubscriptionCurrentPeriodEndKey = "driver_subscription_period_end";
    private const string SubscriptionCancelAtPeriodEndKey = "driver_subscription_cancel_at_period_end";
    private const string DisplayNameKey = "user_display_name";
    private const string EmailKey = "user_email";
    private const string ImageUrlKey = "user_image_url";
    private const string FullNameKey = "profile_full_name";
    private const string PhoneNumberKey = "profile_phone_number";

    public async Task<UserBootstrapState> GetStateAsync()
    {
        var userId = await SecureStorage.GetAsync(UserIdKey) ?? string.Empty;
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
            PhoneNumber = await SecureStorage.GetAsync(PhoneNumberKey) ?? string.Empty,
            Email = await SecureStorage.GetAsync(EmailKey) ?? string.Empty
        };
    }

    public async Task SetAuthSessionAsync(string userId, string accessToken, string? refreshToken, string? idToken, string displayName, string email, string imageUrl)
    {
        await SecureStorage.SetAsync(AuthTokenKey, accessToken);
        await SecureStorage.SetAsync(UserIdKey, userId);

        if (string.IsNullOrWhiteSpace(idToken))
        {
            SecureStorage.Remove(IdTokenKey);
        }
        else
        {
            await SecureStorage.SetAsync(IdTokenKey, idToken);
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            SecureStorage.Remove(RefreshTokenKey);
        }
        else
        {
            await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
        }

        await WriteValueOrRemoveAsync(DisplayNameKey, displayName);
        await WriteValueOrRemoveAsync(EmailKey, email);
        await WriteValueOrRemoveAsync(ImageUrlKey, imageUrl);
    }

    public async Task<User?> GetCachedUserAsync(string accessToken)
    {
        var userId = await SecureStorage.GetAsync(UserIdKey);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var fullName = await SecureStorage.GetAsync(FullNameKey);
        var authDisplayName = await SecureStorage.GetAsync(DisplayNameKey);
        var resolvedDisplayName = !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : string.IsNullOrWhiteSpace(authDisplayName)
                ? "Ridebase User"
                : authDisplayName;

        var user = await BuildUserAsync(userId, accessToken, resolvedDisplayName);
        user.Email = await SecureStorage.GetAsync(EmailKey) ?? string.Empty;
        user.ImageUrl = await SecureStorage.GetAsync(ImageUrlKey) ?? string.Empty;
        return user;
    }

    public Task SetCachedDisplayNameAsync(string displayName)
    {
        return WriteValueOrRemoveAsync(DisplayNameKey, displayName);
    }

    public Task ClearSessionAsync()
    {
        SecureStorage.Remove(AuthTokenKey);
        SecureStorage.Remove(IdTokenKey);
        SecureStorage.Remove(UserIdKey);
        SecureStorage.Remove(RefreshTokenKey);

        SecureStorage.Remove(RoleKey);
        SecureStorage.Remove(IsOnboardedKey);
        SecureStorage.Remove(FullNameKey);
        SecureStorage.Remove(PhoneNumberKey);

        SecureStorage.Remove(DisplayNameKey);
        SecureStorage.Remove(EmailKey);
        SecureStorage.Remove(ImageUrlKey);

        SecureStorage.Remove(DriverSubscribedKey);
        SecureStorage.Remove(SubscriptionIdKey);
        SecureStorage.Remove(SubscriptionCustomerIdKey);
        SecureStorage.Remove(SubscriptionStatusKey);
        SecureStorage.Remove(SubscriptionCurrentPeriodStartKey);
        SecureStorage.Remove(SubscriptionCurrentPeriodEndKey);
        SecureStorage.Remove(SubscriptionCancelAtPeriodEndKey);

        return Task.CompletedTask;
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
