using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Places.Common;
using Microsoft.Maui.Controls.Shapes;
using MPowerKit.GoogleMaps;
using Ridebase.Services;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(StartPlace), "startPlace")]
[QueryProperty(nameof(DestinationPlace), "destinationPlace")]
public partial class RideDetailsViewModel : BaseViewModel
{
    [ObservableProperty]
    private PlaceResult startPlace;
    [ObservableProperty]
    private PlaceResult destinationPlace;
    [ObservableProperty]
    private ObservableCollection<Polyline> polylines = [];
    [ObservableProperty]
    private Polyline roadPolyline;
    [ObservableProperty]
    private ObservableCollection<Coordinate> roadCoordinates = [];
    [ObservableProperty]
    private Action<CameraUpdate> _moveCameraAction;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task> _animateCameraFunc;

    private readonly GoogleMaps.DirectionsApi directionsApi;

    public RideDetailsViewModel(GoogleMaps.DirectionsApi _routesDirectionsApi)
    {
        Title = "Ride Details";
        directionsApi = _routesDirectionsApi;
    }

    [RelayCommand]
    public async Task GetDirectionsAsync()
    {
        IsBusy = true;
        try
        {
            var request = new GoogleApi.Entities.Maps.Directions.Request.DirectionsRequest
            {
                //TODO: how to set origin and destination
                Origin = new LocationEx(new CoordinateEx(StartPlace.Geometry.Location.Latitude, StartPlace.Geometry.Location.Longitude)),
                Destination = new LocationEx(new CoordinateEx(DestinationPlace.Geometry.Location.Latitude, DestinationPlace.Geometry.Location.Longitude)),
                Key = Constants.googleMapsApiKey,
            };

            var routesDirectionsApiResponse = await directionsApi.QueryAsync(request);

            if (routesDirectionsApiResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok)) 
            {
            }

            var something = routesDirectionsApiResponse.Routes.FirstOrDefault();

            await DrawRouteAndZoomAsync(something.OverviewPath.Line);
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    //Method to draw on map and show route
    public async Task DrawRouteAndZoomAsync(IEnumerable<Coordinate> coordinates)
    {
        if (RoadCoordinates == null)
            return;

        PointCollection points = new();

        foreach (var item in coordinates)
        {
            points.Add(new Point(item.Latitude, item.Longitude));
        }

        RoadPolyline = new Polyline
        {
            StrokeThickness = 5,
            StrokeLineJoin = PenLineJoin.Round,
            Points = points
        };

        Polylines.Add(RoadPolyline);

        //Create new camera update to move camera to new location that includes the polyline drawn

        var cameraUpdate = CameraUpdateFactory
            .NewLatLngBounds(MapUtils.CalculateBounds(points), 50);

        await MoveCamera(cameraUpdate);
    }

    // Animate and zoom onto the map
    private async Task MoveCamera(CameraUpdate newPosition)
    {
        await AnimateCameraFunc(newPosition, 2000);
    }
}
