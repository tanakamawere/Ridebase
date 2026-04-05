using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

/// <summary>
/// Production SignalR implementation of IRideRealtimeService.
/// Connects to the backend hub at {RidebaseEndpoint}hubs/ride and forwards
/// server-pushed events to the matching C# events on the interface.
/// </summary>
public sealed class SignalRRideRealtimeService : IRideRealtimeService, IAsyncDisposable
{
    private readonly HubConnection _hub;

    public event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    public event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    public event Action<DriverRideRequest>? DriverRideRequestReceived;
    public event Action<DriverLocationUpdate>? DriverLocationUpdated;

    public SignalRRideRealtimeService(IConfiguration configuration)
    {
        var endpoint = configuration["RidebaseEndpoint"]
            ?? throw new InvalidOperationException("RidebaseEndpoint is not configured in appsettings.json.");

        var hubUrl = new Uri(new Uri(endpoint), "hubs/ride");

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Map inbound hub pushes to interface events
        _hub.On<DriverOfferSelectionModel>("RiderOfferReceived",
            offer => RiderOfferReceived?.Invoke(offer));

        _hub.On<RideStatusUpdateEvent>("RideStatusUpdated",
            evt => RideStatusUpdated?.Invoke(evt));

        _hub.On<DriverRideRequest>("DriverRideRequestReceived",
            req => DriverRideRequestReceived?.Invoke(req));

        _hub.On<DriverLocationUpdate>("DriverLocationUpdated",
            update => DriverLocationUpdated?.Invoke(update));
    }

    // ── Outbound hub invocations ─────────────────────────────────────────────

    public async Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("StartRiderMatching", request, cancellationToken);
    }

    public async Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("AcceptOffer", request, cancellationToken);
    }

    public async Task SubmitDriverOfferAsync(DriverOfferSelectionModel offer, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("SubmitDriverOffer", offer, cancellationToken);
    }

    public async Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("StartDriverRequestStream", driverId, cancellationToken);
    }

    public async Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("UpdateRideStatus", rideId, status, cancellationToken);
    }

    public async Task PublishDriverLocationAsync(DriverLocationUpdate locationUpdate, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("PublishDriverLocation", locationUpdate, cancellationToken);
    }

    public async Task CompleteRideAsync(string rideId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _hub.InvokeAsync("CompleteRide", rideId, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_hub.State != HubConnectionState.Disconnected)
        {
            await _hub.StopAsync(cancellationToken);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_hub.State == HubConnectionState.Disconnected)
        {
            await _hub.StartAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
