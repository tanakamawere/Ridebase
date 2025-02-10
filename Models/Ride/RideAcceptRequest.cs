namespace Ridebase.Models.Ride;

public class RideAcceptRequest
{
    // Unique identifier for the ride
    public string RideId { get; set; }
    // Unique identifier for the driver
    public Guid DriverId { get; set; }
    // Unique identifier for the rider
    public string RiderId { get; set; }
    // Amount offered by the rider
    public decimal OfferAmount { get; set; }
    // Status of the ride
    public RideStatus Status { get; set; }
    // Start location of the ride
    public Location StartLocation { get; set; }
    // Destination location of the ride
    public Location DestinationLocation { get; set; }
}
