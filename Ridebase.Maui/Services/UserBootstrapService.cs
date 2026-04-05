using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class UserBootstrapService : IUserBootstrapService
{
    private readonly IUserSessionService userSessionService;
    private readonly IOnboardingApiClient onboardingApiClient;
    private readonly IPaymentSubscriptionApiClient paymentSubscriptionApiClient;

    public UserBootstrapService(
        IUserSessionService userSessionService,
        IOnboardingApiClient onboardingApiClient,
        IPaymentSubscriptionApiClient paymentSubscriptionApiClient)
    {
        this.userSessionService = userSessionService;
        this.onboardingApiClient = onboardingApiClient;
        this.paymentSubscriptionApiClient = paymentSubscriptionApiClient;
    }

    public async Task<UserBootstrapState> ResolveAfterLoginAsync(string userId)
    {
        var state = await userSessionService.GetStateAsync();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            state.UserId = userId;
        }

        if (string.IsNullOrWhiteSpace(state.UserId))
        {
            state.UserId = Guid.NewGuid().ToString("N");
        }

        var profileResponse = await onboardingApiClient.GetCurrentProfileAsync();

        if (!profileResponse.IsSuccess || profileResponse.Data is null)
        {
            await userSessionService.SetOnboardedAsync(false);
            await userSessionService.ClearSubscriptionStateAsync();

            state = await userSessionService.GetStateAsync();
            state.IsOnboarded = false;
            state.IsDriverSubscribed = false;
            return state;
        }

        await userSessionService.SetOnboardedAsync(true);

        var role = string.Equals(profileResponse.Data.Role, "DRIVER", StringComparison.OrdinalIgnoreCase)
            ? AppUserRole.Driver
            : AppUserRole.Rider;

        await userSessionService.SetRoleAsync(role);
        await userSessionService.SetProfileAsync(profileResponse.Data.FullName, profileResponse.Data.PhoneNumber);
        await userSessionService.SetCachedDisplayNameAsync(profileResponse.Data.FullName);

        state = await userSessionService.GetStateAsync();

        if (state.Role == AppUserRole.Driver)
        {
            var subscriptionResponse = await paymentSubscriptionApiClient.GetSubscriptionStatusAsync();

            if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
            {
                await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
            }
            else
            {
                await userSessionService.ClearSubscriptionStateAsync();
            }

            state = await userSessionService.GetStateAsync();
        }
        else
        {
            await userSessionService.ClearSubscriptionStateAsync();
            state.IsDriverSubscribed = false;
        }

        return state;
    }
}