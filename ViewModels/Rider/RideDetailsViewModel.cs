using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Places.Common;
using Microsoft.Extensions.Configuration;
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
    private decimal recommendedFare;

    [ObservableProperty]
    private double estimatedDistanceKm;

    [ObservableProperty]
    private int estimatedMinutes;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task> _animateCameraFunc;

    private readonly GoogleMaps.DirectionsApi directionsApi;
    private readonly string _googleMapsApiKey;

    public RideDetailsViewModel(GoogleMaps.DirectionsApi _routesDirectionsApi
                            , IConfiguration configuration
                            , IRideApiClient _rideService
                            , IStorageService storage
                            , IUserSessionService _userSessionService
                            , IRideRealtimeService _rideRealtimeService
                            , IRideStateStore _rideStateStore
                            , ILogger<RideDetailsViewModel> logger)
    {
        Title = "Ride Details";
        Logger = logger;
        directionsApi = _routesDirectionsApi;
        _googleMapsApiKey = configuration["GoogleKeys:MapsApiKey"] ?? string.Empty;
        rideApiClient = _rideService;
        storageService = storage;
        userSessionService = _userSessionService;
        rideRealtimeService = _rideRealtimeService;
        rideStateStore = _rideStateStore;
    }

    [RelayCommand]
    public async Task GetDirectionsAsync()
    {
        Logger.LogInformation("Getting directions from {Start} to {Destination}", StartPlace?.Name, DestinationPlace?.Name);
        IsBusy = true;
        try
        {
            if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
            {
                Logger.LogWarning("Google Maps API key is missing for RideDetails directions.");
                return;
            }

            var request = new GoogleApi.Entities.Maps.Directions.Request.DirectionsRequest
            {
                //TODO: how to set origin and destination
                Origin = new LocationEx(new CoordinateEx(StartPlace.Geometry.Location.Latitude, StartPlace.Geometry.Location.Longitude)),
                Destination = new LocationEx(new CoordinateEx(DestinationPlace.Geometry.Location.Latitude, DestinationPlace.Geometry.Location.Longitude)),
                Key = _googleMapsApiKey,
                DepartureTime = DateTime.Now,
            };

            var routesDirectionsApiResponse = await directionsApi.QueryAsync(request);

            if (routesDirectionsApiResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok))
            {
                var response = routesDirectionsApiResponse.Routes.FirstOrDefault();

                Logger.LogInformation("Directions retrieved successfully, drawing route on map");
                await DrawRouteAndZoomAsync(response.OverviewPath.Line, response.Bounds);
                EstimatedDistanceKm = response.Legs.FirstOrDefault()?.Distance?.Value / 1000d ?? 0;
                EstimatedMinutes = (int)Math.Ceiling((response.Legs.FirstOrDefault()?.Duration?.Value ?? 0) / 60d);
                RecommendedFare = CalculateRecommendedFare(EstimatedDistanceKm, EstimatedMinutes);
                if (OfferAmount <= 0)
                {
                    OfferAmount = RecommendedFare;
                }
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
        if (AnimateCameraFunc is null)
        {
            Logger.LogWarning("AnimateCameraFunc not bound yet — skipping camera animation");
            return;
        }
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
            RiderId = await storageService.GetUserIdAsync() ?? string.Empty,
            StartLocation = new() { latitude = StartPlace.Geometry.Location.Latitude, longitude = StartPlace.Geometry.Location.Longitude },
            DestinationLocation = new() { latitude = DestinationPlace.Geometry.Location.Latitude, longitude = DestinationPlace.Geometry.Location.Longitude },
            OfferAmount = OfferAmount,
            Comments = "Nothing entered"
        };

        Logger.LogInformation("Ride request created with ID: {RideId}", rideRequest.RideGuid);

        try
        {
            var response = await rideApiClient.RequestRide(rideRequest);

            if (response.IsSuccess)
            {
                Logger.LogInformation("Ride request successful, navigating to ride selection page");
                await rideRealtimeService.StartRiderMatchingAsync(rideRequest);
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
            await AppShell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private decimal CalculateRecommendedFare(double distanceKm, int etaMinutes)
    {
        var baseFare = 1.50m;
        var distanceComponent = (decimal)distanceKm * 0.75m;
        var durationComponent = etaMinutes * 0.09m;
        var hour = DateTime.Now.Hour;
        var multiplier = hour is >= 17 and <= 20 ? 1.15m : 1m;
        return decimal.Round((baseFare + distanceComponent + durationComponent) * multiplier, 2);
    }
}
