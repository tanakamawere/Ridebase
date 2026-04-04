namespace Ridebase.Models.Rider;

public class RiderActivityItem
{
    public required string DriverRouteText { get; set; }
    public required string TimestampText { get; set; }
    public required string PickupText { get; set; }
    public required string DropOffText { get; set; }
    public required string AmountText { get; set; }
    public required string AccentColor { get; set; }
}

public class RiderProfileOption
{
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string IconGlyph { get; set; }
    public required string AccentColor { get; set; }
}

public class RiderWalletTransaction
{
    public required string Title { get; set; }
    public required string TimestampText { get; set; }
    public required string AmountText { get; set; }
    public required string StatusText { get; set; }
    public required string AccentColor { get; set; }
    public required string IconGlyph { get; set; }
}
