namespace Ridebase.Models.Ride;

public class DriverOfferSelectionModel
{
    // Unique identifier for the ride offer
    public Guid RideOfferId { get; set; }
    // Information about the driver making the offer
    public DriverModel? Driver { get; set; }
    // Amount requested by the driver for the ride
    public decimal OfferAmount { get; set; }
    // Distances between the two locations
    public decimal Distance { get; set; }
    // Pickup location of the ride offer
    public Location? PickupLocation { get; set; }
    // Drop-off location of the ride offer
    public Location? DestinationLocation { get; set; }
    // Time when the ride offer was made
    public DateTime OfferTime { get; set; }
}
