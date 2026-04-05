using System;
using System.ComponentModel.DataAnnotations;

namespace Ridebase.Models.Ride;

public class RideRequestModel
{
    public int Id { get; set; }
    public Guid RideGuid { get; set; } = new Guid();
    [Required(ErrorMessage = "RiderId is required")]
    public string RiderId { get; set; } = string.Empty;
    public string RiderName { get; set; } = string.Empty;
    public string RiderPhoneNumber { get; set; } = string.Empty;
    public Location StartLocation { get; set; } = new();
    public string StartAddress { get; set; } = string.Empty;
    public Location DestinationLocation { get; set; } = new();
    public string DestinationAddress { get; set; } = string.Empty;
    [Required(ErrorMessage = "Offer amount is required")]
    public decimal OfferAmount { get; set; }
    public decimal RecommendedAmount { get; set; }
    public double EstimatedDistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool IsOrderingForSomeoneElse { get; set; }
    public string RequestedForName { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public RideStatus Status { get; set; } = RideStatus.Requested;
    public string Comments { get; set; } = string.Empty;
}
