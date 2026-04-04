namespace Ridebase.Models.Ride;

public class DriverLocationUpdate
{
    public string RideId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public Location CurrentLocation { get; set; } = new();
    public int EtaMinutes { get; set; }
    public double DistanceToPickupKm { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
