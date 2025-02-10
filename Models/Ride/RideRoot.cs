using System.ComponentModel.DataAnnotations;

namespace Ridebase.Models.Ride;

public class RideRoot
{
    public string rideGuid { get; set; }
    public string riderId { get; set; }
    public Location startLocation { get; set; }
    public Location destinationLocation { get; set; }
    public string offerAmount { get; set; }
    public string comment { get; set; }
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