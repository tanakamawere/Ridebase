using Ridebase.Models.Subscriptions;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Interfaces;

public interface IPaymentSubscriptionApiClient
{
    Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<SubscriptionCheckoutResponse>> CreateCheckoutSessionAsync(SubscriptionCheckoutRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<DriverSubscriptionStatus>>> ListSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DriverSubscriptionStatus>> CancelSubscriptionAsync(string subscriptionId, SubscriptionCancelRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SubscriptionPortalResponse>> CreatePortalSessionAsync(CancellationToken cancellationToken = default);
}
