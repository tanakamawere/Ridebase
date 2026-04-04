namespace Ridebase.Models;

public class CarDetails
{
    public required string DriverFullName { get; set; }
    public required string DriverPhoneNumber { get; set; }
    public string? DriverPhotoPath { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required string Year { get; set; }
    public required string LicensePlate { get; set; }
    public required string DriverLicenseNumber { get; set; }
    public bool IsAvailable { get; set; }
}
