using Ridebase.Models.Subscriptions;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;
using System.Net.Http.Json;

namespace Ridebase.Services.ApiClients;

public class PaymentSubscriptionApiClient : IPaymentSubscriptionApiClient
{
    private readonly HttpClient httpClient;

    public PaymentSubscriptionApiClient(HttpClient _httpClient)
    {
        httpClient = _httpClient;
    }

    public Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default)
        => SendGetAsync<DriverSubscriptionStatus>("api/v1/subscriptions/status", cancellationToken);

    public Task<ApiResponse<SubscriptionCheckoutResponse>> CreateCheckoutSessionAsync(SubscriptionCheckoutRequest request, CancellationToken cancellationToken = default)
        => SendPostAsync<SubscriptionCheckoutResponse>("api/v1/subscriptions/checkout", request, cancellationToken);

    public Task<ApiResponse<List<DriverSubscriptionStatus>>> ListSubscriptionsAsync(CancellationToken cancellationToken = default)
        => SendGetAsync<List<DriverSubscriptionStatus>>("api/v1/subscriptions/", cancellationToken);

    public Task<ApiResponse<DriverSubscriptionStatus>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => SendGetAsync<DriverSubscriptionStatus>($"api/v1/subscriptions/{subscriptionId}", cancellationToken);

    public Task<ApiResponse<DriverSubscriptionStatus>> CancelSubscriptionAsync(string subscriptionId, SubscriptionCancelRequest request, CancellationToken cancellationToken = default)
        => SendPostAsync<DriverSubscriptionStatus>($"api/v1/subscriptions/{subscriptionId}/cancel", request, cancellationToken);

    public Task<ApiResponse<SubscriptionPortalResponse>> CreatePortalSessionAsync(CancellationToken cancellationToken = default)
        => SendPostAsync<SubscriptionPortalResponse>("api/v1/subscriptions/portal", new { }, cancellationToken);

    private async Task<ApiResponse<T>> SendGetAsync<T>(string url, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(url, cancellationToken);
        return await response.ToApiResponseAsync<T>();
    }

    private async Task<ApiResponse<T>> SendPostAsync<T>(string url, object data, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(url, data, cancellationToken);
        return await response.ToApiResponseAsync<T>();
    }
}
