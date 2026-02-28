using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class UserSessionService : IUserSessionService
{
    private const string RoleKey = "user_role";
    private const string IsOnboardedKey = "user_is_onboarded";
    private const string DriverSubscribedKey = "driver_subscription";
    private const string FullNameKey = "profile_full_name";
    private const string PhoneNumberKey = "profile_phone_number";

    public async Task<UserBootstrapState> GetStateAsync()
    {
        var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
        var role = await SecureStorage.GetAsync(RoleKey) ?? AppUserRole.Rider.ToString();
        var onboarded = await SecureStorage.GetAsync(IsOnboardedKey) ?? bool.FalseString;
        var subscribed = await SecureStorage.GetAsync(DriverSubscribedKey) ?? bool.FalseString;

        return new UserBootstrapState
        {
            UserId = userId,
            Role = Enum.TryParse<AppUserRole>(role, true, out var parsedRole) ? parsedRole : AppUserRole.Rider,
            IsOnboarded = bool.TryParse(onboarded, out var parsedOnboarded) && parsedOnboarded,
            IsDriverSubscribed = bool.TryParse(subscribed, out var parsedSubscribed) && parsedSubscribed,
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
}
