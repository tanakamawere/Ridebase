namespace Ridebase.Models.Ride;

public class DriverOfferSelectionModel
{
    public Guid RideOfferId { get; set; }
    public string RideId { get; set; } = string.Empty;
    public DriverModel? Driver { get; set; }
    public decimal OfferAmount { get; set; }
    public decimal RiderOfferAmount { get; set; }
    public decimal RecommendedAmount { get; set; }
    public bool IsCounterOffer { get; set; }
    public int EtaToPickupMinutes { get; set; }
    public decimal Distance { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public Location? PickupLocation { get; set; }
    public Location? DestinationLocation { get; set; }
    public DateTime OfferTime { get; set; }

    public string OfferTypeLabel => IsCounterOffer ? "COUNTER OFFER" : "ACCEPTED OFFER";
    public decimal FareDelta => decimal.Round(OfferAmount - RiderOfferAmount, 2);
}
