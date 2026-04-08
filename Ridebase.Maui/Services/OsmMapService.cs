using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

public class OsmMapService : IMapService
{
    private readonly HttpClient _httpClient;

    public OsmMapService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Ridebase-App/1.0");
    }

    public async Task<IEnumerable<PlacePrediction>> GetAutocompleteAsync(string query)
    {
        try
        {
            // Using Nominatim Search for autocomplete
            var url = $"{Constants.OsmAutocompleteUrl}?q={Uri.EscapeDataString(query)}&format=json&limit=5&addressdetails=1";
            var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);

            if (response == null) return Enumerable.Empty<PlacePrediction>();

            return response.Select(r => new PlacePrediction
            {
                PlaceId = r.OsmId.ToString(), // We'll use OSM ID as the "PlaceId"
                MainText = r.DisplayName.Split(',')[0],
                SecondaryText = string.Join(',', r.DisplayName.Split(',').Skip(1)).Trim(),
                Description = r.DisplayName
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OSM Autocomplete Error: {ex.Message}");
            return Enumerable.Empty<PlacePrediction>();
        }
    }

    public async Task<PlaceDetails> GetPlaceDetailsAsync(string placeId)
    {
        try
        {
            // Nominatim Lookup by OSM ID
            // Note: In a real world, we might need to store the type (Node/Way/Relation)
            // For now, we'll search again or use a lookup endpoint if available.
            // Simplified: Nominatim "lookup" uses rtype+id (e.g. N123, W456)
            var url = $"http://{Constants.LocalHostIp}:8081/lookup?osm_ids=N{placeId},W{placeId},R{placeId}&format=json";
            var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);

            var result = response?.FirstOrDefault();
            if (result == null) return new PlaceDetails();

            return new PlaceDetails
            {
                Name = result.DisplayName.Split(',')[0],
                Address = result.DisplayName,
                Latitude = double.Parse(result.Lat),
                Longitude = double.Parse(result.Lon)
            };
        }
        catch
        {
            return new PlaceDetails();
        }
    }

    public async Task<PlaceDetails> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"http://{Constants.LocalHostIp}:8081/reverse?lat={latitude}&lon={longitude}&format=json";
            var result = await _httpClient.GetFromJsonAsync<NominatimResult>(url);

            if (result == null) return new PlaceDetails 
            { 
                Name = "Current Location",
                Address = $"{latitude:F4}, {longitude:F4}",
                Latitude = latitude, 
                Longitude = longitude 
            };

            return new PlaceDetails
            {
                Name = result.DisplayName.Split(',')[0],
                Address = result.DisplayName,
                Latitude = latitude,
                Longitude = longitude
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OSM Reverse Geocode Error: {ex.Message}");
            return new PlaceDetails 
            { 
                Name = "Current Location",
                Address = $"{latitude:F4}, {longitude:F4}",
                Latitude = latitude, 
                Longitude = longitude 
            };
        }
    }

    public async Task<RouteInfo?> GetDirectionsAsync(double startLat, double startLng, double endLat, double endLng)
    {
        try
        {
            var url = $"http://{Constants.LocalHostIp}:5001/route/v1/driving/{startLng},{startLat};{endLng},{endLat}?overview=simplified";
            var response = await _httpClient.GetFromJsonAsync<OsrmResponse>(url);

            var route = response?.Routes?.FirstOrDefault();
            if (route == null) return null;

            return new RouteInfo
            {
                EncodedPolyline = route.Geometry,
                DistanceKm = route.Distance / 1000.0,
                DurationMinutes = route.Duration / 60.0
            };
        }
        catch
        {
            return null;
        }
    }

    // --- Helper Models ---

    private class NominatimResult
    {
        [JsonPropertyName("place_id")] public long PlaceId { get; set; }
        [JsonPropertyName("osm_id")] public long OsmId { get; set; }
        [JsonPropertyName("display_name")] public string DisplayName { get; set; } = string.Empty;
        [JsonPropertyName("lat")] public string Lat { get; set; } = "0";
        [JsonPropertyName("lon")] public string Lon { get; set; } = "0";
    }

    private class OsrmResponse
    {
        [JsonPropertyName("routes")] public List<OsrmRoute>? Routes { get; set; }
    }

    private class OsrmRoute
    {
        [JsonPropertyName("geometry")] public string Geometry { get; set; } = string.Empty;
        [JsonPropertyName("distance")] public double Distance { get; set; }
        [JsonPropertyName("duration")] public double Duration { get; set; }
    }
}
