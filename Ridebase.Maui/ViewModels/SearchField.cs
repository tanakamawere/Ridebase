namespace Ridebase.ViewModels;

/// <summary>
/// Identifies which search field is currently active in the dual-input search UI.
/// </summary>
public enum SearchField
{
    /// <summary>User is editing the pickup / start location.</summary>
    Pickup,

    /// <summary>User is editing the destination.</summary>
    Destination
}
