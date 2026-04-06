using Ridebase.Models;
using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

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
        // Read state once — avoid redundant SecureStorage round-trips
        var state = await userSessionService.GetStateAsync();

        if (!string.IsNullOrWhiteSpace(userId))
            state.UserId = userId;
        if (string.IsNullOrWhiteSpace(state.UserId))
            state.UserId = Guid.NewGuid().ToString("N");

        // For returning drivers, start subscription check in parallel with profile fetch
        var profileTask = onboardingApiClient.GetCurrentProfileAsync();
        Task<ApiResponse<DriverSubscriptionStatus>>? earlySubTask =
            (state.Role == AppUserRole.Driver && state.IsOnboarded)
                ? paymentSubscriptionApiClient.GetSubscriptionStatusAsync()
                : null;

        var profileResponse = await profileTask;

        if (!profileResponse.IsSuccess || profileResponse.Data is null)
        {
            await Task.WhenAll(
                userSessionService.SetOnboardedAsync(false),
                userSessionService.ClearSubscriptionStateAsync()
            );
            state.IsOnboarded = false;
            state.IsDriverSubscribed = false;
            return state;
        }

        var role = string.Equals(profileResponse.Data.Role, "DRIVER", StringComparison.OrdinalIgnoreCase)
            ? AppUserRole.Driver
            : AppUserRole.Rider;

        // Parallelize storage writes
        await Task.WhenAll(
            userSessionService.SetOnboardedAsync(true),
            userSessionService.SetRoleAsync(role),
            userSessionService.SetProfileAsync(profileResponse.Data.FullName, profileResponse.Data.PhoneNumber),
            userSessionService.SetCachedDisplayNameAsync(profileResponse.Data.FullName)
        );

        // Update in-memory state instead of re-reading from storage
        state.IsOnboarded = true;
        state.Role = role;
        state.FullName = profileResponse.Data.FullName;
        state.PhoneNumber = profileResponse.Data.PhoneNumber;

        if (role == AppUserRole.Driver)
        {
            // Await early subscription task, or start it now if role changed to driver
            var subscriptionResponse = await (earlySubTask ?? paymentSubscriptionApiClient.GetSubscriptionStatusAsync());

            if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
            {
                await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
                state.IsDriverSubscribed = subscriptionResponse.Data.IsSubscribed;
                state.SubscriptionId = subscriptionResponse.Data.SubscriptionId;
                state.CustomerId = subscriptionResponse.Data.CustomerId;
                state.SubscriptionStatus = subscriptionResponse.Data.Status;
                state.SubscriptionCurrentPeriodStart = subscriptionResponse.Data.CurrentPeriodStart;
                state.SubscriptionCurrentPeriodEnd = subscriptionResponse.Data.CurrentPeriodEnd;
                state.SubscriptionCancelAtPeriodEnd = subscriptionResponse.Data.CancelAtPeriodEnd;
            }
            else
            {
                await userSessionService.ClearSubscriptionStateAsync();
                state.IsDriverSubscribed = false;
            }
        }
        else
        {
            await userSessionService.ClearSubscriptionStateAsync();
            state.IsDriverSubscribed = false;
        }

        return state;
    }
}