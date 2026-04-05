namespace Ridebase.Models.Ride;

public class RideRatingRequest
{
    public string RideId { get; set; } = string.Empty;
    public string RiderId { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public int Rating { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
