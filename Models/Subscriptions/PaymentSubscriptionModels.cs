using System.Text.Json.Serialization;

namespace Ridebase.Models.Subscriptions;

public class DriverSubscriptionStatus
{
    [JsonPropertyName("is_subscribed")]
    public bool IsSubscribed { get; set; }

    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("current_period_start")]
    public long? CurrentPeriodStart { get; set; }

    [JsonPropertyName("current_period_end")]
    public long? CurrentPeriodEnd { get; set; }

    [JsonPropertyName("cancel_at_period_end")]
    public bool? CancelAtPeriodEnd { get; set; }
}

public class SubscriptionCheckoutRequest
{
    [JsonPropertyName("success_url")]
    public string? SuccessUrl { get; set; }

    [JsonPropertyName("cancel_url")]
    public string? CancelUrl { get; set; }
}

public class SubscriptionCheckoutResponse
{
    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
}

public class SubscriptionPortalResponse
{
    [JsonPropertyName("portal_url")]
    public string? PortalUrl { get; set; }
}

public class SubscriptionCancelRequest
{
    [JsonPropertyName("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; set; }
}