using Ridebase.Services.RideService;

namespace Ridebase.Services.DriverServices;

public class DriverModels
{
}

public class DriverRideRequest
{
    // Unique identifier for the ride request
    public Guid RideRequestId { get; set; }

    // Information about the rider making the request
    public Rider? Rider { get; set; }
    // Amount offered by the rider for the ride
    public decimal OfferAmount { get; set; }
    // Distances between the two locations
    public decimal Distance { get; set; }

    // Pickup location of the ride request
    public Location? PickupLocation { get; set; }

    // Drop-off location of the ride request
    public Location? DestinationLocation { get; set; }

    // Time when the ride request was made
    public DateTime RequestTime { get; set; }
}
