namespace Ridebase.Models;

/// <summary>
/// Simplified model for a Google Places Autocomplete prediction.
/// Decouples the UI from the raw GoogleApi response type.
/// </summary>
public class PlacePrediction
{
    /// <summary>Google Place ID — used to fetch full details / coordinates.</summary>
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>Primary display text (e.g. "Sam Levy's Village").</summary>
    public string MainText { get; set; } = string.Empty;

    /// <summary>Secondary display text (e.g. "Borrowdale, Harare, Zimbabwe").</summary>
    public string SecondaryText { get; set; } = string.Empty;

    /// <summary>Full formatted description.</summary>
    public string Description { get; set; } = string.Empty;
}
