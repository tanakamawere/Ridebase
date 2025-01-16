namespace Ridebase.Services.RideService;

public class RideModels
{
}

public class RideRoot
{
    public string rideGuid { get; set; }
    public string riderId { get; set; }
    public Location startLocation { get; set; }
    public Location destinationLocation { get; set; }
    public string offerAmount { get; set; }
    public string comment { get; set; }
}

public class Rider
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
}