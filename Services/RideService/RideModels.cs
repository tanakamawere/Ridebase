using System.ComponentModel.DataAnnotations;

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

public class RideRequest
{
    public int Id { get; set; }
    public Guid RideGuid { get; set; } = new Guid();
    [Required(ErrorMessage = "RiderId is required")]
    public string RiderId { get; set; }
    public Location StartLocation { get; set; }
    public Location DestinationLocation { get; set; }
    //Add modifier to limit amount to 2dp
    [Required(ErrorMessage = "Offer amount is required")]
    public decimal OfferAmount { get; set; }
    //Optional comments for ride
    public string Comments { get; set; }
}

public class RideRequestResponse
{
    public string RideRequestId { get; set; }
    public RideStatus RideStatus { get; set; }
    public double RideDistance { get; set; }
    public int EstimatedWaitTime { get; set; }
}

public enum RideStatus
{
    Pending,
    InTransit,
    Done,
    Cancelled,
    Offer_Accepted,
    Offer_Rejected
}