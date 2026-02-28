namespace Ridebase.Models.Ride;

public class RideSessionModel
{
    public string RideId { get; set; } = string.Empty;
    public string RiderId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public string RiderPhoneNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhoneNumber { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public Location StartLocation { get; set; } = new();
    public Location DestinationLocation { get; set; } = new();
    public decimal RiderOfferAmount { get; set; }
    public decimal AcceptedAmount { get; set; }
    public double DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Requested;
}
