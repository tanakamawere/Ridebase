using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class MockRideRealtimeService : IRideRealtimeService
{
    private readonly IUserSessionService userSessionService;
    private RideRequestModel? currentRiderRequest;
    private readonly Random random = new();
    private readonly Dictionary<string, RideAcceptRequest> acceptedRides = [];

    private readonly List<string> _onlineDriverIds = [];

    public event Action<DriverOfferSelectionModel>? RiderOfferReceived;
    public event Action<RideStatusUpdateEvent>? RideStatusUpdated;
    public event Action<DriverRideRequest>? DriverRideRequestReceived;
    public event Action<DriverLocationUpdate>? DriverLocationUpdated;

    public MockRideRealtimeService(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
    }

    public async Task StartRiderMatchingAsync(RideRequestModel request, CancellationToken cancellationToken = default)
    {
        currentRiderRequest = request;

        var seedOffers = new[]
        {
            BuildOffer("Tino", "+263771111111", "Toyota Aqua", 4.8, request.OfferAmount + 0.75m, true, 8),
            BuildOffer("Rudo", "+263772222222", "Honda Fit", 4.7, request.OfferAmount, false, 6),
            BuildOffer("Simba", "+263773333333", "Mazda Demio", 4.9, request.OfferAmount + 1.10m, true, 5)
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

        foreach (var onlineDriverId in _onlineDriverIds)
        {
            PushRequestToDriver(onlineDriverId);
        }
    }

    public Task AcceptOfferAsync(RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        acceptedRides[request.RideId] = request;
        RideStatusUpdated?.Invoke(new RideStatusUpdateEvent
        {
            RideId = request.RideId,
            Status = RideStatus.DriverEnRoute,
            EtaMinutes = 6,
            StatusMessage = "Driver selected. Heading to pickup now."
        });
        return Task.CompletedTask;
    }

    public Task SubmitDriverOfferAsync(DriverOfferSelectionModel offer, CancellationToken cancellationToken = default)
    {
        RiderOfferReceived?.Invoke(offer);
        return Task.CompletedTask;
    }

    public async Task StartDriverRequestStreamAsync(string driverId, CancellationToken cancellationToken = default)
    {
        var state = await userSessionService.GetStateAsync();
        if (!state.IsDriverSubscribed)
        {
            return;
        }

        if (!_onlineDriverIds.Contains(driverId))
            _onlineDriverIds.Add(driverId);

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
            RiderName = string.IsNullOrWhiteSpace(currentRiderRequest.RiderName) ? "Kinetic Rider" : currentRiderRequest.RiderName,
            RiderPhoneNumber = currentRiderRequest.RiderPhoneNumber,
            OfferAmount = currentRiderRequest.OfferAmount,
            RecommendedAmount = currentRiderRequest.RecommendedAmount,
            PickupAddress = currentRiderRequest.StartAddress,
            DestinationAddress = currentRiderRequest.DestinationAddress,
            EtaToPickupMinutes = 6,
            DistanceToPickupKm = 2.3m,
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
            EtaMinutes = status == RideStatus.DriverEnRoute ? 6 : 0,
            StatusMessage = status switch
            {
                RideStatus.DriverArrived => "Your driver has arrived at pickup.",
                RideStatus.TripStarted => "Your trip is now in progress.",
                RideStatus.TripCompleted => "Trip completed successfully.",
                RideStatus.Cancelled => "Ride cancelled.",
                _ => "Ride updated."
            },
            UpdatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task PublishDriverLocationAsync(DriverLocationUpdate locationUpdate, CancellationToken cancellationToken = default)
    {
        DriverLocationUpdated?.Invoke(locationUpdate);
        return Task.CompletedTask;
    }

    public Task CompleteRideAsync(string rideId, CancellationToken cancellationToken = default)
    {
        RideStatusUpdated?.Invoke(new RideStatusUpdateEvent
        {
            RideId = rideId,
            Status = RideStatus.TripCompleted,
            StatusMessage = "Trip completed successfully.",
            UpdatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _onlineDriverIds.Clear();
        return Task.CompletedTask;
    }

    private DriverOfferSelectionModel BuildOffer(string driverName, string phone, string vehicle, double rating, decimal amount, bool isCounterOffer, int etaToPickupMinutes)
    {
        return new DriverOfferSelectionModel
        {
            RideOfferId = Guid.NewGuid(),
            RideId = currentRiderRequest?.RideGuid.ToString("N") ?? string.Empty,
            OfferAmount = decimal.Round(amount, 2),
            RiderOfferAmount = currentRiderRequest?.OfferAmount ?? amount,
            RecommendedAmount = currentRiderRequest?.RecommendedAmount ?? amount,
            IsCounterOffer = isCounterOffer,
            EtaToPickupMinutes = etaToPickupMinutes,
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
            PickupAddress = currentRiderRequest?.StartAddress ?? "Pickup",
            DestinationAddress = currentRiderRequest?.DestinationAddress ?? "Destination",
            PickupLocation = currentRiderRequest?.StartLocation,
            DestinationLocation = currentRiderRequest?.DestinationLocation,
            OfferTime = DateTime.UtcNow
        };
    }
}
