using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling.Layers;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class RideDetailsPage : ContentPage
{
	private readonly RideDetailsViewModel rideDetailsViewModel;
	public RideDetailsPage(RideDetailsViewModel _rideDetailsViewModel)
    {
        InitializeComponent();
        BindingContext = rideDetailsViewModel = _rideDetailsViewModel;
        rideDetailsViewModel.OnRequestMapUpdate += OnRequestMapUpdate;
        InitializeOsmMap();
	}

    private void InitializeOsmMap()
    {
        var map = new Mapsui.Map();
        
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(), 
            Constants.OsmTileUrl, 
            name: "Self-Hosted OSM");
            
        var tileLayer = new TileLayer(tileSource) { Name = "Self-Hosted OSM" };
        map.Layers.Add(tileLayer);

        // Center on Zimbabwe or initial point
        var (x, y) = SphericalMercator.FromLonLat(31.05, -17.82);
        map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolution: 200);

        MapControl.Map = map;
    }

    private void OnRequestMapUpdate(object? sender, MapUpdateEventArgs e)
    {
        var mapControl = MapControl;
        if (mapControl == null) return;

        switch (e.Type)
        {
            case MapUpdateType.Camera:
                var (x, y) = SphericalMercator.FromLonLat(e.Longitude, e.Latitude);
                mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), e.Zoom > 0 ? e.Zoom : 38);
                break;
            case MapUpdateType.Route:
                if (!string.IsNullOrEmpty(e.RoutePolyline))
                {
                    // Draw polyline logic...
                }
                break;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (rideDetailsViewModel.EstimatedDistanceKm == 0)
        {
            await rideDetailsViewModel.GetDirectionsAsync();
        }
    }
}