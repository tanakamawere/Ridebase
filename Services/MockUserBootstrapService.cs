using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class MockUserBootstrapService : IUserBootstrapService
{
    private readonly IUserSessionService userSessionService;
    private readonly IPaymentSubscriptionApiClient paymentSubscriptionApiClient;

    public MockUserBootstrapService(IUserSessionService _userSessionService, IPaymentSubscriptionApiClient _paymentSubscriptionApiClient)
    {
        userSessionService = _userSessionService;
        paymentSubscriptionApiClient = _paymentSubscriptionApiClient;
    }

    public async Task<UserBootstrapState> ResolveAfterLoginAsync(string userId)
    {
        var state = await userSessionService.GetStateAsync();
        state.UserId = userId;

        if (string.IsNullOrWhiteSpace(state.UserId))
        {
            state.UserId = Guid.NewGuid().ToString("N");
        }

        if (state.Role == AppUserRole.Driver && state.IsOnboarded)
        {
            var subscriptionResponse = await paymentSubscriptionApiClient.GetSubscriptionStatusAsync();

            if (subscriptionResponse.IsSuccess && subscriptionResponse.Data is not null)
            {
                await userSessionService.SetSubscriptionStateAsync(subscriptionResponse.Data);
                state = await userSessionService.GetStateAsync();
            }
            else
            {
                state.IsDriverSubscribed = false;
            }
        }
        else if (state.Role == AppUserRole.Driver)
        {
            state.IsDriverSubscribed = false;
        }

        return state;
    }
}
