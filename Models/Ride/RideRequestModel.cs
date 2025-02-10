using System;
using System.ComponentModel.DataAnnotations;

namespace Ridebase.Models.Ride;

public class RideRequestModel
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
