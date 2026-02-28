using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Places.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;
using MPowerKit.GoogleMaps;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;
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
    private decimal offerAmount = 2;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task> _animateCameraFunc;

    private readonly GoogleMaps.DirectionsApi directionsApi;

    public RideDetailsViewModel(GoogleMaps.DirectionsApi _routesDirectionsApi
                            , IRideApiClient _rideService
                            , IStorageService storage
                            , ILogger<RideDetailsViewModel> logger)
    {
        Title = "Ride Details";
        Logger = logger;
        directionsApi = _routesDirectionsApi;
        rideApiClient = _rideService;
        storageService = storage;
    }

    [RelayCommand]
    public async Task GetDirectionsAsync()
    {
        Logger.LogInformation("Getting directions from {Start} to {Destination}", StartPlace?.Name, DestinationPlace?.Name);
        IsBusy = true;
        try
        {
            var request = new GoogleApi.Entities.Maps.Directions.Request.DirectionsRequest
            {
                //TODO: how to set origin and destination
                Origin = new LocationEx(new CoordinateEx(StartPlace.Geometry.Location.Latitude, StartPlace.Geometry.Location.Longitude)),
                Destination = new LocationEx(new CoordinateEx(DestinationPlace.Geometry.Location.Latitude, DestinationPlace.Geometry.Location.Longitude)),
                Key = Constants.googleMapsApiKey,
                DepartureTime = DateTime.Now,
            };

            var routesDirectionsApiResponse = await directionsApi.QueryAsync(request);

            if (routesDirectionsApiResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok))
            {
                var response = routesDirectionsApiResponse.Routes.FirstOrDefault();

                Logger.LogInformation("Directions retrieved successfully, drawing route on map");
                await DrawRouteAndZoomAsync(response.OverviewPath.Line, response.Bounds);
            }
            else
            {
                Logger.LogWarning("Directions API returned status: {Status}", routesDirectionsApiResponse.Status);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting directions");
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    //Method to draw on map and show route
    public async Task DrawRouteAndZoomAsync(IEnumerable<Coordinate> coordinates, GoogleApi.Entities.Common.ViewPort viewport)
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
            .NewLatLngBounds(MapUtils.GetLatLngBoundsFromViewPort(viewport), 50);

        await MoveCamera(cameraUpdate);
    }

    // Animate and zoom onto the map
    private async Task MoveCamera(CameraUpdate newPosition)
    {
        await AnimateCameraFunc(newPosition, 2000);
    }

    // Method to find driver from api after sending a ride request object
    [RelayCommand]
    public async Task FindDriverAsync()
    {
        Logger.LogInformation("Starting driver search");
        IsBusy = true;
        //if (!await storageService.IsLoggedInAsync())
        //{
        //    //TODO: Open dialog to inform user they need to be logged in to make a ride request
        //    return;
        //}


        RideRequestModel rideRequest = new()
        {
            RideGuid = Guid.NewGuid(),
            RiderId = "RidebaseUser.UserId",
            StartLocation = new() { latitude = StartPlace.Geometry.Location.Latitude, longitude = StartPlace.Geometry.Location.Longitude },
            DestinationLocation = new() { latitude = DestinationPlace.Geometry.Location.Latitude, longitude = DestinationPlace.Geometry.Location.Longitude },
            OfferAmount = 0,
            Comments = "Nothing entered"
        };

        Logger.LogInformation("Ride request created with ID: {RideId}", rideRequest.RideGuid);

        try
        {
            var response = await rideApiClient.RequestRide(rideRequest);

            if (response.IsSuccess)
            {
                Logger.LogInformation("Ride request successful, navigating to ride selection page");
                //TODO: open popup for driver found
                await Shell.Current.GoToAsync(nameof(RideSelectionPage), true, new Dictionary<string, object> 
                {
                    {"rideRequest", rideRequest }
                });
            }
            else
            {
                Logger.LogWarning("Ride request failed: {ErrorMessage}", response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error requesting ride");
            // Display alert
            await AppShell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
