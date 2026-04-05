namespace Ridebase.Models.Ride;

public class RideAcceptRequest
{
    public string RideId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public Guid RideOfferId { get; set; }
    public string RiderId { get; set; } = string.Empty;
    public decimal OfferAmount { get; set; }
    public decimal RecommendedAmount { get; set; }
    public RideStatus Status { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
}
