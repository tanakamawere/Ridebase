using Microsoft.Extensions.Configuration;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ridebase.Services;

/// <summary>
/// Production raw-WebSocket implementation of <see cref="IRideRealtimeService"/>.
/// Connects to <c>{RidebaseEndpoint}</c> (https → wss, http → ws) + <c>ws/ride</c>.
///
/// Wire protocol — every message is a JSON envelope:
/// <code>{ "type": "EventName", "payload": { ... } }</code>
///
/// Inbound types  : RiderOfferReceived | RideStatusUpdated | DriverRideRequestReceived
/// Outbound types : StartRiderMatching | AcceptOffer | StartDriverRequestStream | UpdateRideStatus | Stop
/// </summary>
public sealed class WebSocketRideRealtimeService : IRideRealtimeService, IAsyncDisposable
{
    private readonly Uri _wsUri;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _receiveCts;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    public event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    public event Action<DriverRideRequest>? DriverRideRequestReceived;
    public event Action<DriverLocationUpdate>? DriverLocationUpdated;

    public WebSocketRideRealtimeService(IConfiguration configuration)
    {
        var endpoint = configuration["RidebaseEndpoint"]
            ?? throw new InvalidOperationException("RidebaseEndpoint is not configured in appsettings.json.");

        // Convert https → wss, http → ws and append the hub path
        var wsScheme = endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        var hostAndPath = endpoint
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        _wsUri = new Uri($"{wsScheme}://{hostAndPath}/ws/ride");
    }

    // ── Connection lifecycle ─────────────────────────────────────────────────

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_ws?.State == WebSocketState.Open) return;

        _ws?.Dispose();
        _ws = new ClientWebSocket();
        _receiveCts = new CancellationTokenSource();

        await _ws.ConnectAsync(_wsUri, cancellationToken);

        // Start background receive loop
        _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);
    }

    // ── Outbound messages ────────────────────────────────────────────────────

    public async Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("StartRiderMatching", request, cancellationToken);
    }

    public async Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("AcceptOffer", request, cancellationToken);
    }

    public async Task SubmitDriverOfferAsync(DriverOfferSelectionModel offer, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("SubmitDriverOffer", offer, cancellationToken);
    }

    public async Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("StartDriverRequestStream", new { driverId }, cancellationToken);
    }

    public async Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("UpdateRideStatus", new { rideId, status }, cancellationToken);
    }

    public async Task PublishDriverLocationAsync(DriverLocationUpdate locationUpdate, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("PublishDriverLocation", locationUpdate, cancellationToken);
    }

    public async Task CompleteRideAsync(string rideId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await SendAsync("CompleteRide", new { rideId }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _receiveCts?.Cancel();

        if (_ws?.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client stopping", cancellationToken);
        }
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private async Task SendAsync<T>(string type, T payload, CancellationToken cancellationToken)
    {
        var envelope = new WsEnvelope<T>(type, payload);
        var json = JsonSerializer.Serialize(envelope, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 8];

        try
        {
            while (_ws?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                // Accumulate frames until end-of-message
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                DispatchMessage(Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (WebSocketException) { /* connection dropped — caller should reconnect */ }
    }

    private void DispatchMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();

            if (!root.TryGetProperty("payload", out var payload)) return;
            var payloadJson = payload.GetRawText();

            switch (type)
            {
                case "RiderOfferReceived":
                    var offer = JsonSerializer.Deserialize<DriverOfferSelectionModel>(payloadJson, JsonOpts);
                    if (offer is not null) RiderOfferReceived?.Invoke(offer);
                    break;

                case "RideStatusUpdated":
                    var evt = JsonSerializer.Deserialize<RideStatusUpdateEvent>(payloadJson, JsonOpts);
                    if (evt is not null) RideStatusUpdated?.Invoke(evt);
                    break;

                case "DriverRideRequestReceived":
                    var req = JsonSerializer.Deserialize<DriverRideRequest>(payloadJson, JsonOpts);
                    if (req is not null) DriverRideRequestReceived?.Invoke(req);
                    break;

                case "DriverLocationUpdated":
                    var location = JsonSerializer.Deserialize<DriverLocationUpdate>(payloadJson, JsonOpts);
                    if (location is not null) DriverLocationUpdated?.Invoke(location);
                    break;
            }
        }
        catch (JsonException) { /* malformed message — skip */ }
    }

    public async ValueTask DisposeAsync()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();

        if (_ws is not null)
        {
            if (_ws.State == WebSocketState.Open)
            {
                try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None); }
                catch { /* ignore on disposal */ }
            }
            _ws.Dispose();
        }
    }

    // ── Wire protocol types ───────────────────────────────────────────────────

    private sealed record WsEnvelope<T>(
        [property: JsonPropertyName("type")]    string Type,
        [property: JsonPropertyName("payload")] T Payload);
}
