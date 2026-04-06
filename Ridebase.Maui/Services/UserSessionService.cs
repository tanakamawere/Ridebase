using Ridebase.Models;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class UserSessionService : IUserSessionService
{
    private const string AuthTokenKey = "auth_token";
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
        // Fire all reads in parallel
        var userIdTask = SecureStorage.GetAsync(UserIdKey);
        var roleTask = SecureStorage.GetAsync(RoleKey);
        var onboardedTask = SecureStorage.GetAsync(IsOnboardedKey);
        var subStatusTask = SecureStorage.GetAsync(SubscriptionStatusKey);
        var subscribedTask = SecureStorage.GetAsync(DriverSubscribedKey);
        var periodStartTask = SecureStorage.GetAsync(SubscriptionCurrentPeriodStartKey);
        var periodEndTask = SecureStorage.GetAsync(SubscriptionCurrentPeriodEndKey);
        var cancelAtEndTask = SecureStorage.GetAsync(SubscriptionCancelAtPeriodEndKey);
        var subIdTask = SecureStorage.GetAsync(SubscriptionIdKey);
        var customerIdTask = SecureStorage.GetAsync(SubscriptionCustomerIdKey);
        var fullNameTask = SecureStorage.GetAsync(FullNameKey);
        var phoneTask = SecureStorage.GetAsync(PhoneNumberKey);

        await Task.WhenAll(
            userIdTask, roleTask, onboardedTask, subStatusTask,
            subscribedTask, periodStartTask, periodEndTask, cancelAtEndTask,
            subIdTask, customerIdTask, fullNameTask, phoneTask);

        var subscriptionStatus = subStatusTask.Result ?? string.Empty;
        var subscribed = subscribedTask.Result ?? bool.FalseString;

        return new UserBootstrapState
        {
            UserId = userIdTask.Result ?? string.Empty,
            Role = Enum.TryParse<AppUserRole>(roleTask.Result ?? AppUserRole.Rider.ToString(), true, out var parsedRole) ? parsedRole : AppUserRole.Rider,
            IsOnboarded = bool.TryParse(onboardedTask.Result ?? bool.FalseString, out var parsedOnboarded) && parsedOnboarded,
            IsDriverSubscribed = IsSubscribedStatus(subscriptionStatus) || (bool.TryParse(subscribed, out var parsedSubscribed) && parsedSubscribed),
            SubscriptionId = subIdTask.Result,
            CustomerId = customerIdTask.Result,
            SubscriptionStatus = subscriptionStatus,
            SubscriptionCurrentPeriodStart = long.TryParse(periodStartTask.Result, out var parsedPeriodStart) ? parsedPeriodStart : null,
            SubscriptionCurrentPeriodEnd = long.TryParse(periodEndTask.Result, out var parsedPeriodEnd) ? parsedPeriodEnd : null,
            SubscriptionCancelAtPeriodEnd = bool.TryParse(cancelAtEndTask.Result, out var parsedCancelAtPeriodEnd) ? parsedCancelAtPeriodEnd : null,
            FullName = fullNameTask.Result ?? string.Empty,
            PhoneNumber = phoneTask.Result ?? string.Empty
        };
    }

    public async Task SetAuthSessionAsync(string userId, string accessToken, string? refreshToken, string displayName, string email, string imageUrl)
    {
        var tasks = new List<Task>
        {
            SecureStorage.SetAsync(AuthTokenKey, accessToken),
            SecureStorage.SetAsync(UserIdKey, userId),
            WriteValueOrRemoveAsync(DisplayNameKey, displayName),
            WriteValueOrRemoveAsync(EmailKey, email),
            WriteValueOrRemoveAsync(ImageUrlKey, imageUrl)
        };

        if (string.IsNullOrWhiteSpace(refreshToken))
            SecureStorage.Remove(RefreshTokenKey);
        else
            tasks.Add(SecureStorage.SetAsync(RefreshTokenKey, refreshToken));

        await Task.WhenAll(tasks);
    }

    public async Task<User?> GetCachedUserAsync(string accessToken)
    {
        // Fire all reads in parallel
        var userIdTask = SecureStorage.GetAsync(UserIdKey);
        var fullNameTask = SecureStorage.GetAsync(FullNameKey);
        var displayNameTask = SecureStorage.GetAsync(DisplayNameKey);
        var emailTask = SecureStorage.GetAsync(EmailKey);
        var imageUrlTask = SecureStorage.GetAsync(ImageUrlKey);

        await Task.WhenAll(userIdTask, fullNameTask, displayNameTask, emailTask, imageUrlTask);

        var userId = userIdTask.Result;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var fullName = fullNameTask.Result;
        var authDisplayName = displayNameTask.Result;
        var resolvedDisplayName = !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : string.IsNullOrWhiteSpace(authDisplayName)
                ? "Ridebase User"
                : authDisplayName;

        var user = await BuildUserAsync(userId, accessToken, resolvedDisplayName);
        user.Email = emailTask.Result ?? string.Empty;
        user.ImageUrl = imageUrlTask.Result ?? string.Empty;
        return user;
    }

    public Task SetCachedDisplayNameAsync(string displayName)
    {
        return WriteValueOrRemoveAsync(DisplayNameKey, displayName);
    }

    public Task ClearSessionAsync()
    {
        SecureStorage.Remove(AuthTokenKey);
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
        await Task.WhenAll(
            SecureStorage.SetAsync(DriverSubscribedKey, isSubscribed.ToString()),
            SecureStorage.SetAsync(SubscriptionStatusKey, isSubscribed ? "active" : "canceled")
        );
    }

    public async Task SetSubscriptionStateAsync(DriverSubscriptionStatus subscriptionState)
    {
        await Task.WhenAll(
            SecureStorage.SetAsync(DriverSubscribedKey, subscriptionState.IsSubscribed.ToString()),
            WriteValueOrRemoveAsync(SubscriptionIdKey, subscriptionState.SubscriptionId),
            WriteValueOrRemoveAsync(SubscriptionCustomerIdKey, subscriptionState.CustomerId),
            WriteValueOrRemoveAsync(SubscriptionStatusKey, subscriptionState.Status),
            WriteValueOrRemoveAsync(SubscriptionCurrentPeriodStartKey, subscriptionState.CurrentPeriodStart?.ToString()),
            WriteValueOrRemoveAsync(SubscriptionCurrentPeriodEndKey, subscriptionState.CurrentPeriodEnd?.ToString()),
            WriteValueOrRemoveAsync(SubscriptionCancelAtPeriodEndKey, subscriptionState.CancelAtPeriodEnd?.ToString())
        );
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
        await Task.WhenAll(
            SecureStorage.SetAsync(FullNameKey, fullName),
            SecureStorage.SetAsync(PhoneNumberKey, phoneNumber)
        );
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
