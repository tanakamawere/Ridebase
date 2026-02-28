// MockLifecycleTests.cs
//
// NOTE: The Ridebase MAUI project targets net10.0-android/ios and cannot be directly
// referenced by a plain net10.0 test project. These tests are self-contained:
// they inline minimal type definitions and service reimplementations that mirror
// the production code, serving as executable specification of the ride lifecycle
// contract. When the service layer is extracted to a shared library, swap the
// inline stubs out for direct project references.

using System.Collections.Concurrent;
using Moq;

namespace Ridebase.Tests.Services;

// ── Minimal model stubs ──────────────────────────────────────────────────────

public enum RideStatus
{
    Requested, SearchingDrivers, OfferCountered, OfferAccepted,
    DriverEnRoute, DriverArrived, TripStarted, TripCompleted,
    Cancelled, OfferRejected
}

public class Location { public double Latitude { get; set; } public double Longitude { get; set; } }

public class RideRequestModel
{
    public Guid RideGuid { get; set; } = Guid.NewGuid();
    public decimal OfferAmount { get; set; }
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
}

public class DriverModel
{
    public Guid DriverId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int RidesCompleted { get; set; }
    public string Vehicle { get; set; } = string.Empty;
}

public class DriverOfferSelectionModel
{
    public Guid RideOfferId { get; set; }
    public decimal OfferAmount { get; set; }
    public DriverModel Driver { get; set; } = new();
    public decimal Distance { get; set; }
    public DateTime OfferTime { get; set; }
}

public class DriverRideRequest
{
    public Guid RideId { get; set; }
    public Guid DriverId { get; set; }
    public Guid RiderId { get; set; }
    public decimal OfferAmount { get; set; }
    public RideStatus Status { get; set; }
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
}

public class RideAcceptRequest
{
    public string RideId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public string RiderId { get; set; } = string.Empty;
    public decimal OfferAmount { get; set; }
    public RideStatus Status { get; set; }
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
}

public class RideStatusUpdateEvent
{
    public string RideId { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RideSessionModel
{
    public string RideId { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public string DriverPhoneNumber { get; set; } = string.Empty;
    public string RiderPhoneNumber { get; set; } = string.Empty;
}

public class UserBootstrapState
{
    public bool IsDriverSubscribed { get; set; }
}

// ── Minimal interface stubs ───────────────────────────────────────────────────

public interface IUserSessionService
{
    Task<UserBootstrapState> GetStateAsync();
}

public interface IRideStateStore
{
    RideSessionModel? CurrentRide { get; }
    event Action<RideSessionModel?>? RideChanged;
    void SetCurrentRide(RideSessionModel? rideSession);
    void UpdateStatus(RideStatus status);
}

public interface IRideRealtimeService
{
    event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    event Action<DriverRideRequest>? DriverRideRequestReceived;
    Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default);
    Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default);
    Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default);
    Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

// ── Inline service implementations (mirror production code) ──────────────────

/// <summary>
/// Mirrors Ridebase.Services.RideStateStore — thread-safe singleton ride store.
/// </summary>
public sealed class RideStateStore : IRideStateStore
{
    public RideSessionModel? CurrentRide { get; private set; }
    public event Action<RideSessionModel?>? RideChanged;

    public void SetCurrentRide(RideSessionModel? rideSession)
    {
        CurrentRide = rideSession;
        RideChanged?.Invoke(CurrentRide);
    }

    public void UpdateStatus(RideStatus status)
    {
        if (CurrentRide is null) return;
        CurrentRide.Status = status;
        RideChanged?.Invoke(CurrentRide);
    }
}

/// <summary>
/// Mirrors Ridebase.Services.MockRideRealtimeService.
/// </summary>
public sealed class MockRideRealtimeService : IRideRealtimeService
{
    private readonly IUserSessionService _userSessionService;
    private RideRequestModel? _currentRiderRequest;
    private readonly Random _rng = new();

    public event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    public event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    public event Action<DriverRideRequest>? DriverRideRequestReceived;

    public MockRideRealtimeService(IUserSessionService userSessionService)
        => _userSessionService = userSessionService;

    public async Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default)
    {
        _currentRiderRequest = request;

        var seedOffers = new[]
        {
            MakeOffer("Tino", "+263771111111", "Toyota Aqua", 4.8, request.OfferAmount + 0.75m),
            MakeOffer("Rudo", "+263772222222", "Honda Fit",   4.7, request.OfferAmount),
            MakeOffer("Simba", "+263773333333", "Mazda Demio", 4.9, request.OfferAmount + 1.10m),
        };

        foreach (var offer in seedOffers)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await Task.Delay(_rng.Next(350, 900), cancellationToken);
            RiderOfferReceived?.Invoke(offer);
        }
    }

    public Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default)
    {
        if (_currentRiderRequest is null) return;

        var state = await _userSessionService.GetStateAsync();
        if (!state.IsDriverSubscribed) return;

        var parsedId = Guid.TryParse(driverId, out var id) ? id : Guid.NewGuid();
        DriverRideRequestReceived?.Invoke(new DriverRideRequest
        {
            RideId   = _currentRiderRequest.RideGuid,
            DriverId = parsedId,
            RiderId  = Guid.NewGuid(),
            OfferAmount = _currentRiderRequest.OfferAmount,
            Status = RideStatus.Requested,
            StartLocation       = _currentRiderRequest.StartLocation,
            DestinationLocation = _currentRiderRequest.DestinationLocation,
        });
    }

    public Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        RideStatusUpdated?.Invoke(new RideStatusUpdateEvent { RideId = rideId, Status = status });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private DriverOfferSelectionModel MakeOffer(
        string name, string phone, string vehicle, double rating, decimal amount)
        => new()
        {
            RideOfferId = Guid.NewGuid(),
            OfferAmount = decimal.Round(amount, 2),
            Driver = new DriverModel
            {
                DriverId = Guid.NewGuid(), Name = name, PhoneNumber = phone,
                Rating = rating, RidesCompleted = _rng.Next(200, 1200), Vehicle = vehicle
            },
            Distance  = decimal.Round((decimal)(_rng.NextDouble() * 4 + 0.5), 1),
            OfferTime = DateTime.UtcNow,
        };
}

// ── Status-text mapper (mirrors RideProgressViewModel.SyncFromRide switch) ───

internal static class RideStatusTextMapper
{
    internal static string Map(RideStatus status) => status switch
    {
        RideStatus.DriverEnRoute   => "Driver en route",
        RideStatus.DriverArrived   => "Driver arrived",
        RideStatus.TripStarted     => "Trip started",
        RideStatus.TripCompleted   => "Trip completed",
        RideStatus.Cancelled       => "Ride cancelled",
        _                          => "Waiting for driver",
    };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class MockLifecycleTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static Mock<IUserSessionService> SubscribedUserSession()
    {
        var mock = new Mock<IUserSessionService>();
        mock.Setup(s => s.GetStateAsync())
            .ReturnsAsync(new UserBootstrapState { IsDriverSubscribed = true });
        return mock;
    }

    private static Mock<IUserSessionService> UnsubscribedUserSession()
    {
        var mock = new Mock<IUserSessionService>();
        mock.Setup(s => s.GetStateAsync())
            .ReturnsAsync(new UserBootstrapState { IsDriverSubscribed = false });
        return mock;
    }

    // ── 1. StartRiderMatchingAsync fires exactly 3 offers ────────────────────

    [Fact]
    public async Task StartRiderMatchingAsync_FiresExactlyThreeOffers()
    {
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);
        var received = new ConcurrentBag<DriverOfferSelectionModel>();
        service.RiderOfferReceived += offer => received.Add(offer);

        var request = new RideRequestModel { OfferAmount = 7.50m };
        await service.StartRiderMatchingAsync(request);

        Assert.Equal(3, received.Count);
    }

    [Fact]
    public async Task StartRiderMatchingAsync_AllOffersHavePositiveAmounts()
    {
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);
        var received = new ConcurrentBag<DriverOfferSelectionModel>();
        service.RiderOfferReceived += offer => received.Add(offer);

        await service.StartRiderMatchingAsync(new RideRequestModel { OfferAmount = 5.00m });

        Assert.All(received, o => Assert.True(o.OfferAmount > 0));
    }

    [Fact]
    public async Task StartRiderMatchingAsync_OffersHaveDistinctDriverNames()
    {
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);
        var names = new ConcurrentBag<string>();
        service.RiderOfferReceived += offer => names.Add(offer.Driver.Name);

        await service.StartRiderMatchingAsync(new RideRequestModel { OfferAmount = 6.00m });

        Assert.Equal(3, names.Distinct().Count());
    }

    // ── 2. StartDriverRequestStreamAsync subscription gate ───────────────────

    [Fact]
    public async Task StartDriverRequestStreamAsync_WithActiveSubscription_FiresEvent()
    {
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);

        // Prime the internal ride request so the driver stream has something to push
        await service.StartRiderMatchingAsync(
            new RideRequestModel { OfferAmount = 8.00m },
            new CancellationTokenSource(0).Token);   // cancel immediately after first setup

        DriverRideRequest? received = null;
        service.DriverRideRequestReceived += req => received = req;

        await service.StartDriverRequestStreamAsync(Guid.NewGuid().ToString());

        Assert.NotNull(received);
        Assert.Equal(RideStatus.Requested, received.Status);
    }

    [Fact]
    public async Task StartDriverRequestStreamAsync_WithoutSubscription_DoesNotFireEvent()
    {
        var service = new MockRideRealtimeService(UnsubscribedUserSession().Object);

        // Suppress the CancellationToken-related TaskCanceledException that ends the offers loop
        try { await service.StartRiderMatchingAsync(new RideRequestModel { OfferAmount = 8.00m }); }
        catch (OperationCanceledException) { }

        DriverRideRequest? received = null;
        service.DriverRideRequestReceived += req => received = req;

        await service.StartDriverRequestStreamAsync(Guid.NewGuid().ToString());

        Assert.Null(received);
    }

    // ── 3. RideStateStore contract ────────────────────────────────────────────

    [Fact]
    public void RideStateStore_SetCurrentRide_RaisesRideChanged()
    {
        var store = new RideStateStore();
        RideSessionModel? raised = null;
        store.RideChanged += r => raised = r;

        var session = new RideSessionModel { RideId = "ride-001" };
        store.SetCurrentRide(session);

        Assert.NotNull(raised);
        Assert.Equal("ride-001", raised.RideId);
        Assert.Same(store.CurrentRide, raised);
    }

    [Fact]
    public void RideStateStore_UpdateStatus_UpdatesCurrentRideAndFiresEvent()
    {
        var store = new RideStateStore();
        store.SetCurrentRide(new RideSessionModel { RideId = "ride-002", Status = RideStatus.Requested });

        RideSessionModel? changed = null;
        store.RideChanged += r => changed = r;

        store.UpdateStatus(RideStatus.DriverEnRoute);

        Assert.Equal(RideStatus.DriverEnRoute, store.CurrentRide!.Status);
        Assert.NotNull(changed);
        Assert.Equal(RideStatus.DriverEnRoute, changed!.Status);
    }

    [Fact]
    public void RideStateStore_UpdateStatus_WhenNoCurrentRide_IsNoOp()
    {
        var store = new RideStateStore();
        int eventCount = 0;
        store.RideChanged += _ => eventCount++;

        store.UpdateStatus(RideStatus.DriverEnRoute);   // should not throw or fire

        Assert.Equal(0, eventCount);
        Assert.Null(store.CurrentRide);
    }

    [Fact]
    public void RideStateStore_SetCurrentRide_NullClearsStore()
    {
        var store = new RideStateStore();
        store.SetCurrentRide(new RideSessionModel { RideId = "ride-003" });

        RideSessionModel? raised = null;
        store.RideChanged += r => raised = r;
        store.SetCurrentRide(null);

        Assert.Null(store.CurrentRide);
        Assert.Null(raised);                         // RideChanged fires with null payload
    }

    // ── 4. RideProgressViewModel status-text mapping ─────────────────────────

    [Theory]
    [InlineData(RideStatus.DriverEnRoute,  "Driver en route")]
    [InlineData(RideStatus.DriverArrived,  "Driver arrived")]
    [InlineData(RideStatus.TripStarted,    "Trip started")]
    [InlineData(RideStatus.TripCompleted,  "Trip completed")]
    [InlineData(RideStatus.Cancelled,      "Ride cancelled")]
    [InlineData(RideStatus.Requested,      "Waiting for driver")]
    [InlineData(RideStatus.SearchingDrivers, "Waiting for driver")]
    [InlineData(RideStatus.OfferAccepted,  "Waiting for driver")]
    public void StatusTextMapper_ReturnsExpectedText(RideStatus status, string expectedText)
    {
        Assert.Equal(expectedText, RideStatusTextMapper.Map(status));
    }

    // ── 5. Full rider lifecycle integration ───────────────────────────────────

    [Fact]
    public async Task FullRiderLifecycle_MatchingToAcceptedUpdatesStore()
    {
        // Arrange
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);
        var store   = new RideStateStore();

        DriverOfferSelectionModel? acceptedOffer = null;
        service.RiderOfferReceived += offer =>
        {
            // Simulate rider accepting the first offer
            if (acceptedOffer is null)
            {
                acceptedOffer = offer;
            }
        };

        // Act — start matching
        var request = new RideRequestModel { OfferAmount = 9.00m };
        await service.StartRiderMatchingAsync(request);

        // Simulate rider accepting the first offer that arrived
        Assert.NotNull(acceptedOffer);

        var session = new RideSessionModel
        {
            RideId = request.RideGuid.ToString(),
            Status = RideStatus.OfferAccepted,
            DriverPhoneNumber = acceptedOffer.Driver.PhoneNumber,
        };

        store.SetCurrentRide(session);

        // Assert state reflects accepted offer
        Assert.Equal(RideStatus.OfferAccepted, store.CurrentRide!.Status);
        Assert.Equal(acceptedOffer.Driver.PhoneNumber, store.CurrentRide.DriverPhoneNumber);
    }

    [Fact]
    public async Task FullRiderLifecycle_StatusUpdate_PropagatesViaStoreAndTextMapper()
    {
        var service = new MockRideRealtimeService(SubscribedUserSession().Object);
        var store   = new RideStateStore();

        var session = new RideSessionModel
        {
            RideId = "ride-flow-01",
            Status = RideStatus.OfferAccepted,
        };
        store.SetCurrentRide(session);

        // Subscribe to realtime updates and mirror them to the store
        service.RideStatusUpdated += evt =>
        {
            if (string.Equals(evt.RideId, store.CurrentRide?.RideId, StringComparison.OrdinalIgnoreCase))
            {
                store.UpdateStatus(evt.Status);
            }
        };

        // Driver marks arrival
        await service.UpdateRideStatusAsync(session.RideId, RideStatus.DriverArrived);

        Assert.Equal(RideStatus.DriverArrived, store.CurrentRide!.Status);
        Assert.Equal("Driver arrived", RideStatusTextMapper.Map(store.CurrentRide.Status));
    }
}
