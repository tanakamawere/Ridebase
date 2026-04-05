using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class MockPaymentSubscriptionApiClient : IPaymentSubscriptionApiClient
{
    private readonly IUserSessionService userSessionService;

    public MockPaymentSubscriptionApiClient(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
    }

    public async Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default)
    {
        var state = await userSessionService.GetStateAsync();
        return new ApiResponse<DriverSubscriptionStatus>
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = new DriverSubscriptionStatus
            {
                IsSubscribed = state.IsDriverSubscribed,
                SubscriptionId = state.SubscriptionId,
                CustomerId = state.CustomerId,
                Status = state.SubscriptionStatus,
                CurrentPeriodStart = state.SubscriptionCurrentPeriodStart,
                CurrentPeriodEnd = state.SubscriptionCurrentPeriodEnd,
                CancelAtPeriodEnd = state.SubscriptionCancelAtPeriodEnd
            }
        };
    }

    public async Task<ApiResponse<SubscriptionCheckoutResponse>> CreateCheckoutSessionAsync(SubscriptionCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var activeState = new DriverSubscriptionStatus
        {
            IsSubscribed = true,
            SubscriptionId = "sub_mock_ridebase",
            CustomerId = "cus_mock_ridebase",
            Status = "active",
            CurrentPeriodStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds(),
            CancelAtPeriodEnd = false
        };

        await userSessionService.SetSubscriptionStateAsync(activeState);

        return new ApiResponse<SubscriptionCheckoutResponse>
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = new SubscriptionCheckoutResponse
            {
                CheckoutUrl = request.SuccessUrl ?? "https://ridebase.tech/subscription/mock-checkout",
                SessionId = "cs_mock_ridebase"
            }
        };
    }

    public async Task<ApiResponse<List<DriverSubscriptionStatus>>> ListSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetSubscriptionStatusAsync(cancellationToken);
        return new ApiResponse<List<DriverSubscriptionStatus>>
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = [status.Data!]
        };
    }

    public Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => GetSubscriptionStatusAsync(cancellationToken);

    public async Task<ApiResponse<DriverSubscriptionStatus>> CancelSubscriptionAsync(string subscriptionId, SubscriptionCancelRequest request, CancellationToken cancellationToken = default)
    {
        var canceledState = new DriverSubscriptionStatus
        {
            IsSubscribed = false,
            SubscriptionId = subscriptionId,
            CustomerId = "cus_mock_ridebase",
            Status = "canceled",
            CurrentPeriodStart = DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(),
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            CancelAtPeriodEnd = request.CancelAtPeriodEnd
        };

        await userSessionService.SetSubscriptionStateAsync(canceledState);

        return new ApiResponse<DriverSubscriptionStatus>
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = canceledState
        };
    }

    public Task<ApiResponse<SubscriptionPortalResponse>> CreatePortalSessionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ApiResponse<SubscriptionPortalResponse>
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = new SubscriptionPortalResponse
            {
                PortalUrl = "https://billing.stripe.com/p/mock-session"
            }
        });
}
