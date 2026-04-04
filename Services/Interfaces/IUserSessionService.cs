using Ridebase.Models;
using Ridebase.Models.Subscriptions;

namespace Ridebase.Services.Interfaces;

public interface IUserSessionService
{
    Task<UserBootstrapState> GetStateAsync();
    Task SetRoleAsync(AppUserRole role);
    Task SetOnboardedAsync(bool isOnboarded);
    Task SetDriverSubscriptionAsync(bool isSubscribed);
    Task SetSubscriptionStateAsync(DriverSubscriptionStatus subscriptionState);
    Task ClearSubscriptionStateAsync();
    Task SetProfileAsync(string fullName, string phoneNumber);
    Task<User> BuildUserAsync(string userId, string accessToken, string displayName);
}
