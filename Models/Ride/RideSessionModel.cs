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
    public string StartAddress { get; set; } = string.Empty;
    public Location DestinationLocation { get; set; } = new();
    public string DestinationAddress { get; set; } = string.Empty;
    public decimal RiderOfferAmount { get; set; }
    public decimal RecommendedAmount { get; set; }
    public decimal AcceptedAmount { get; set; }
    public Guid SelectedOfferId { get; set; }
    public double DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public int DriverEtaMinutes { get; set; }
    public Location DriverCurrentLocation { get; set; } = new();
    public string DriverStatusNote { get; set; } = string.Empty;
    public int? RiderRating { get; set; }
    public string RiderFeedback { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Requested;
}
