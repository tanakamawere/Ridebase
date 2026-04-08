using Ridebase.Models;

namespace Ridebase.Services.Interfaces;

public interface IMapService
{
    Task<IEnumerable<PlacePrediction>> GetAutocompleteAsync(string query);
    Task<PlaceDetails> GetPlaceDetailsAsync(string placeId);
    Task<PlaceDetails> ReverseGeocodeAsync(double latitude, double longitude);
    Task<RouteInfo?> GetDirectionsAsync(double startLat, double startLng, double endLat, double endLng);
}

public class RouteInfo
{
    public string EncodedPolyline { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public double DurationMinutes { get; set; }
}

public class PlaceDetails
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
