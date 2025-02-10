namespace Ridebase.Models.Ride;

public class DriverModel
{
    // Unique identifier for the driver
    public Guid DriverId { get; set; }
    // Name of the driver
    public string Name { get; set; }
    // Phone number of the driver
    public string PhoneNumber { get; set; }
    // Rating of the driver
    public double Rating { get; set; }
    // Number of rides completed by the driver
    public int RidesCompleted { get; set; }
    // Vehicle information of the driver
    public string? Vehicle { get; set; }
}
