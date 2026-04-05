using Ridebase.Models;
using Ridebase.Models.Subscriptions;

namespace Ridebase.Services.Interfaces;

public interface IUserSessionService
{
    Task<UserBootstrapState> GetStateAsync();
    Task SetAuthSessionAsync(string userId, string accessToken, string? refreshToken, string displayName, string email, string imageUrl);
    Task<User?> GetCachedUserAsync(string accessToken);
    Task SetCachedDisplayNameAsync(string displayName);
    Task ClearSessionAsync();
    Task SetRoleAsync(AppUserRole role);
    Task SetOnboardedAsync(bool isOnboarded);
    Task SetDriverSubscriptionAsync(bool isSubscribed);
    Task SetSubscriptionStateAsync(DriverSubscriptionStatus subscriptionState);
    Task ClearSubscriptionStateAsync();
    Task SetProfileAsync(string fullName, string phoneNumber);
    Task<User> BuildUserAsync(string userId, string accessToken, string displayName);
}
