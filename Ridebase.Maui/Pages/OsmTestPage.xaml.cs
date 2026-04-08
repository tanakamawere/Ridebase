using Mapsui;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.Layers;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Projections;
using System.Text.Json;
using System.Net.Http;

namespace Ridebase.Pages;

public partial class OsmTestPage : ContentPage
{
    // CHANGE THIS TO YOUR MAC'S IP (e.g., "192.168.1.15") FOR PHYSICAL DEVICES
    // "10.0.2.2" IS THE SPECIAL IP FOR THE ANDROID EMULATOR TO ACCESS THE HOST MACHINE
    private const string ServerIP = "10.0.2.2"; 

    private readonly HttpClient _httpClient = new();

    public OsmTestPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        var map = new Mapsui.Map();
        
        // 1. Add your self-hosted OSM Tiles
        string tileUrl = $"http://{ServerIP}:8080/tile/{{z}}/{{x}}/{{y}}.png";
        
        // In Mapsui v5, we create a TileSource and then a TileLayer
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(), 
            tileUrl, 
            name: "Self-Hosted OSM");
            
        var tileLayer = new TileLayer(tileSource) { Name = "Self-Hosted OSM" };
            
        map.Layers.Add(tileLayer);

        // 2. Center on Zimbabwe (Harare approximately -17.82, 31.05)
        var (x, y) = SphericalMercator.FromLonLat(31.05, -17.82);
        map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolution: 38.2); // Resolution 38.2 is roughly zoom level 12

        // 3. Add some basic widgets
        map.Widgets.Enqueue(new ScaleBarWidget(map) { TextColor = Mapsui.Styles.Color.Gray });

        mapView.Map = map;
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        string query = SearchEntry.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        SearchStatus.Text = "Searching Nominatim...";
        
        try
        {
            // Test Nominatim Geocoding
            string url = $"http://{ServerIP}:8081/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            var response = await _httpClient.GetStringAsync(url);
            
            // Just a quick check to see if we got data
            SearchStatus.Text = $"Success! Found: {query}. (Raw data received)";
        }
        catch (Exception ex)
        {
            SearchStatus.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnTestRouteClicked(object sender, EventArgs e)
    {
        RouteStatus.Text = "Querying OSRM...";
        
        try
        {
            // Harare Center to Airport (approximate coords)
            string start = "31.05,-17.82";
            string end = "31.10,-17.92";
            
            string url = $"http://{ServerIP}:5001/route/v1/driving/{start};{end}?overview=simplified";
            var response = await _httpClient.GetStringAsync(url);
            
            RouteStatus.Text = "Success! OSRM returned route data.";
            
            // In a real implementation, you'd parse the geometry and draw it on the map
        }
        catch (Exception ex)
        {
            RouteStatus.Text = $"Error: {ex.Message}";
        }
    }
}
