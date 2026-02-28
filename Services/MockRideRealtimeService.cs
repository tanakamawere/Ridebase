using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class MockRideRealtimeService : IRideRealtimeService
{
    private readonly IUserSessionService userSessionService;
    private RideRequestModel? currentRiderRequest;
    private readonly Random random = new();

    // Drivers that have registered as online (driverId → their subscribed state)
    // Used so that when a rider request arrives AFTER a driver is already online,
    // the driver still receives the push.
    private readonly List<string> _onlineDriverIds = [];

    public event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    public event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    public event Action<DriverRideRequest>? DriverRideRequestReceived;

    public MockRideRealtimeService(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
    }

    public async Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default)
    {
        currentRiderRequest = request;

        var seedOffers = new[]
        {
            BuildOffer("Tino", "+263771111111", "Toyota Aqua", 4.8, request.OfferAmount + 0.75m),
            BuildOffer("Rudo", "+263772222222", "Honda Fit", 4.7, request.OfferAmount),
            BuildOffer("Simba", "+263773333333", "Mazda Demio", 4.9, request.OfferAmount + 1.10m)
        };

        foreach (var offer in seedOffers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(random.Next(350, 900), cancellationToken);
            RiderOfferReceived?.Invoke(offer);
        }

        // Push to any drivers who registered as online before this rider request arrived
        foreach (var onlineDriverId in _onlineDriverIds)
        {
            PushRequestToDriver(onlineDriverId);
        }
    }

    public Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default)
    {
        var state = await userSessionService.GetStateAsync();
        if (!state.IsDriverSubscribed)
        {
            return;
        }

        // Register this driver as online so future rider requests reach them
        if (!_onlineDriverIds.Contains(driverId))
            _onlineDriverIds.Add(driverId);

        // If a rider request is already queued, push it immediately
        if (currentRiderRequest is not null)
        {
            PushRequestToDriver(driverId);
        }
    }

    private void PushRequestToDriver(string driverId)
    {
        if (currentRiderRequest is null) return;
        var parsedDriverId = Guid.TryParse(driverId, out var id) ? id : Guid.NewGuid();
        DriverRideRequestReceived?.Invoke(new DriverRideRequest
        {
            RideId = currentRiderRequest.RideGuid,
            DriverId = parsedDriverId,
            RiderId = Guid.NewGuid(),
            OfferAmount = currentRiderRequest.OfferAmount,
            Status = RideStatus.Requested,
            StartLocation = currentRiderRequest.StartLocation,
            DestinationLocation = currentRiderRequest.DestinationLocation
        });
    }

    public Task UpdateRideStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        RideStatusUpdated?.Invoke(new RideStatusUpdateEvent
        {
            RideId = rideId,
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _onlineDriverIds.Clear();
        return Task.CompletedTask;
    }

    private DriverOfferSelectionModel BuildOffer(string driverName, string phone, string vehicle, double rating, decimal amount)
    {
        return new DriverOfferSelectionModel
        {
            RideOfferId = Guid.NewGuid(),
            OfferAmount = decimal.Round(amount, 2),
            Driver = new DriverModel
            {
                DriverId = Guid.NewGuid(),
                Name = driverName,
                PhoneNumber = phone,
                Rating = rating,
                RidesCompleted = random.Next(200, 1200),
                Vehicle = vehicle
            },
            Distance = decimal.Round((decimal)(random.NextDouble() * 4 + 0.5), 1),
            OfferTime = DateTime.UtcNow
        };
    }
}
