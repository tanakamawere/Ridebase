namespace Ridebase.Models.Ride;

public class DriverRideRequest
{
    public Guid RideId { get; set; }
    public Guid DriverId { get; set; }
    public Guid RiderId { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public string RiderPhoneNumber { get; set; } = string.Empty;
    public decimal OfferAmount { get; set; }
    public decimal RecommendedAmount { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public int EtaToPickupMinutes { get; set; }
    public decimal DistanceToPickupKm { get; set; }
    public RideStatus Status { get; set; }
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
}
