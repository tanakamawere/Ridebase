namespace Ridebase.Models.Ride;

public class DriverSosRequest
{
    public string RideId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string RiderId { get; set; } = string.Empty;
    public string RiderName { get; set; } = string.Empty;
    public RideStatus TripStatus { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Location CurrentLocation { get; set; } = new();
    public DateTimeOffset TriggeredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
