using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(StartAddress), "startAddress")]
[QueryProperty(nameof(DestinationAddress), "destinationAddress")]
[QueryProperty(nameof(StartLat), "startLat")]
[QueryProperty(nameof(StartLng), "startLng")]
[QueryProperty(nameof(DestLat), "destLat")]
[QueryProperty(nameof(DestLng), "destLng")]
public partial class RideDetailsViewModel : BaseViewModel
{
    [ObservableProperty]
    private string startAddress = string.Empty;
    [ObservableProperty]
    private string destinationAddress = string.Empty;
    
    // We'll pass these via properties or simplified models
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double DestLat { get; set; }
    public double DestLng { get; set; }
    [ObservableProperty]
    private decimal offerAmount = 2;

    [ObservableProperty]
    private decimal recommendedFare;

    [ObservableProperty]
    private double estimatedDistanceKm;

    [ObservableProperty]
    private int estimatedMinutes;

    private readonly IMapService _mapService;

    public RideDetailsViewModel(
        IMapService mapService,
        IConfiguration configuration,
        IRideApiClient _rideService,
        IStorageService storage,
        IUserSessionService _userSessionService,
        IRideRealtimeService _rideRealtimeService,
        IRideStateStore _rideStateStore,
        ILogger<RideDetailsViewModel> logger)
    {
        Title = "Ride Details";
        Logger = logger;
        _mapService = mapService;
        rideApiClient = _rideService;
        storageService = storage;
        userSessionService = _userSessionService;
        rideRealtimeService = _rideRealtimeService;
        rideStateStore = _rideStateStore;
    }

    [RelayCommand]
    public async Task GetDirectionsAsync()
    {
        Logger.LogInformation("Getting directions from {Start} to {Destination}", StartAddress, DestinationAddress);
        IsBusy = true;
        try
        {
            var routeInfo = await _mapService.GetDirectionsAsync(StartLat, StartLng, DestLat, DestLng);

            if (routeInfo != null)
            {
                EstimatedDistanceKm = routeInfo.DistanceKm;
                EstimatedMinutes = (int)Math.Ceiling(routeInfo.DurationMinutes);
                RecommendedFare = CalculateRecommendedFare(EstimatedDistanceKm, EstimatedMinutes);
                if (OfferAmount <= 0) OfferAmount = RecommendedFare;

                OnRequestMapUpdate?.Invoke(this, new MapUpdateEventArgs 
                { 
                    Type = MapUpdateType.Route, 
                    RoutePolyline = routeInfo.EncodedPolyline 
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting directions");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event EventHandler<MapUpdateEventArgs>? OnRequestMapUpdate;

    public async Task AnimateToLocation(double lat, double lng, double zoom)
    {
        OnRequestMapUpdate?.Invoke(this, new MapUpdateEventArgs 
        { 
            Type = MapUpdateType.Camera, 
            Latitude = lat, 
            Longitude = lng, 
            Zoom = zoom 
        });
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
            StartLocation = new() { latitude = StartLat, longitude = StartLng },
            DestinationLocation = new() { latitude = DestLat, longitude = DestLng },
            OfferAmount = OfferAmount,
            Comments = "Nothing entered",
            StartAddress = StartAddress,
            DestinationAddress = DestinationAddress
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
