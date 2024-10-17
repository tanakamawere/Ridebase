using System.ComponentModel.DataAnnotations;

namespace Ridebase.Models;

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
