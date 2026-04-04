namespace Ridebase.Models.Ride;

public class RideStatusUpdateEvent
{
    public string RideId { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public int EtaMinutes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
