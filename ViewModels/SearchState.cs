namespace Ridebase.ViewModels;

/// <summary>
/// Represents the current state of the inline search flow on the HomePage bottom sheet.
/// </summary>
public enum SearchState
{
    /// <summary>Default — quick actions, current location, "Where to?" CTA.</summary>
    Idle,

    /// <summary>User is typing a destination — autocomplete results shown.</summary>
    PickingDestination,

    /// <summary>Destination selected — route polyline, distance/ETA/fare displayed.</summary>
    RoutePreview,

    /// <summary>Ride request sent — waiting for driver offers.</summary>
    FindingDriver
}
