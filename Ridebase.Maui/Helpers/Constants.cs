namespace Ridebase.Helpers;

public class Constants
{
    public const string googleMapsApiUrl = "https://maps.googleapis.com/maps/api/geocode/json?";
    public const string googlePlacesApiUrl = "https://places.googleapis.com/v1/places:searchText";
    public const string RidebaseApiUrl = "https://765e-197-221-253-16.ngrok-free.app/";
    public const string GoogleDirectionsApiUrl = "https://maps.googleapis.com/maps/api/directions/json?";

    // --- SELF-HOSTED OSM STACK (VERIFIED) ---
    public static string LocalHostIp => DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "192.168.0.254";
    
    public static string OsmGeocodingUrl => $"http://{LocalHostIp}:8081/search";
    public static string OsmRoutingUrl => $"http://{LocalHostIp}:5001/route/v1/driving/";
    public static string OsmTileUrl => $"http://{LocalHostIp}:8080/tile/{{z}}/{{x}}/{{y}}.png";
    public static string OsmAutocompleteUrl => $"http://{LocalHostIp}:8081/search"; // Using Nominatim for now, can switch to Photon (2322) if preferred
}
