using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;
using Mopups.Interfaces;
using MPowerKit.GoogleMaps;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels;

public partial class HomePageViewModel : BaseViewModel
{
    // ─── Location state ──────────────────────────────────────────
    [ObservableProperty]
    private LocationWithAddress? currentLocation;

    // ─── Map bindings ────────────────────────────────────────────
    [ObservableProperty]
    private CameraUpdate? _initialCameraPosition;

    [ObservableProperty]
    private Action<CameraUpdate>? _moveCameraAction;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task>? _animateCameraFunc;

    /// <summary>
    /// When the map control binds AnimateCameraFunc, if we already have a
    /// location, immediately animate the camera there.
    /// </summary>
    partial void OnAnimateCameraFuncChanged(Func<CameraUpdate, int, Task>? value)
    {
        if (value is not null && CurrentLocation is not null)
        {
            _ = AnimateToLocation(
                CurrentLocation.Location.latitude,
                CurrentLocation.Location.longitude,
                15);
        }
    }

    // ─── Pickup location (editable, defaults to GPS) ─────────────
    [ObservableProperty]
    private LocationWithAddress? pickupLocation;

    [ObservableProperty]
    private string pickupSearchQuery = string.Empty;

    // ─── Search state ────────────────────────────────────────────
    [ObservableProperty]
    private SearchState currentSearchState = SearchState.Idle;

    [ObservableProperty]
    private SearchField activeSearchField = SearchField.Destination;

    [ObservableProperty]
    private string destinationSearchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PlacePrediction> predictions = [];

    [ObservableProperty]
    private PlacePrediction? selectedPrediction;

    [ObservableProperty]
    private bool isSearching;

    // ─── Selected destination (after geocoding the prediction) ───
    [ObservableProperty]
    private LocationWithAddress? selectedDestination;

    // ─── Route preview state ─────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Polyline> polylines = [];

    [ObservableProperty]
    private Polyline? roadPolyline;

    [ObservableProperty]
    private double estimatedDistanceKm;

    [ObservableProperty]
    private int estimatedMinutes;

    [ObservableProperty]
    private decimal recommendedFare;

    [ObservableProperty]
    private decimal offerAmount = 2;

    // ─── Services ────────────────────────────────────────────────
    private readonly GoogleMaps.Geocode.LocationGeocodeApi _geocodeApi;
    private readonly GoogleMaps.Geocode.AddressGeocodeApi _addressGeocodeApi;
    private readonly GooglePlaces.AutoCompleteApi _autoCompleteApi;
    private readonly GoogleMaps.DirectionsApi _directionsApi;
    private readonly bool _useMockServices;
    private CancellationTokenSource? _searchCts;
    private string? _pendingRideId;

    public HomePageViewModel(
        IPopupNavigation navigation,
        GoogleMaps.Geocode.LocationGeocodeApi locationGeocodeApi,
        GoogleMaps.Geocode.AddressGeocodeApi addressGeocodeApi,
        GooglePlaces.AutoCompleteApi autoCompleteApi,
        GoogleMaps.DirectionsApi directionsApi,
        IStorageService storage,
        IRideApiClient rideApi,
        IRideRealtimeService realtimeService,
        IRideStateStore rideStore,
        IUserSessionService userSession,
        ILogger<HomePageViewModel> logger,
        IConfiguration configuration)
    {
        Title = "Map Page";
        Logger = logger;

        _geocodeApi = locationGeocodeApi;
        _addressGeocodeApi = addressGeocodeApi;
        _autoCompleteApi = autoCompleteApi;
        _directionsApi = directionsApi;

        popupNavigation = navigation;
        storageService = storage;
        rideApiClient = rideApi;
        rideRealtimeService = realtimeService;
        rideStateStore = rideStore;
        userSessionService = userSession;

        _useMockServices = configuration.GetValue<bool>("UseMockServices");

        GetCurrentLocation();
    }

    // ═════════════════════════════════════════════════════════════
    //  LOCATION
    // ═════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool locationPermissionGranted;

    public async Task GetCurrentLocation()
    {
        try
        {
            Logger.LogInformation("Getting current location");
            IsBusy = true;

            var locationService = new LocationService();
            var location = await locationService.GetCurrentLocationAsync();

            LocationPermissionGranted = location != null;

            if (location is null)
            {
                Logger.LogWarning("Geolocation returned null");
                SetFallbackLocation();
                return;
            }

            Logger.LogInformation("Location: {Lat}, {Lng}", location.Latitude, location.Longitude);

            var response = await _geocodeApi.QueryAsync(
                new GoogleApi.Entities.Maps.Geocoding.Location.Request.LocationGeocodeRequest
                {
                    Location = new GoogleApi.Entities.Common.Coordinate(location.Latitude, location.Longitude),
                    Key = Constants.googleMapsApiKey
                });

            var firstResult = response?.Results?.FirstOrDefault();

            CurrentLocation = new LocationWithAddress
            {
                Location = new Models.Location
                {
                    latitude = location.Latitude,
                    longitude = location.Longitude
                },
                FormattedAddress = firstResult?.FormattedAddress ?? $"{location.Latitude:F4}, {location.Longitude:F4}"
            };

            Logger.LogInformation("Current location: {Address}", CurrentLocation.FormattedAddress);
            await AnimateToLocation(CurrentLocation.Location.latitude, CurrentLocation.Location.longitude, 15);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current location");
            SetFallbackLocation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void SetFallbackLocation()
    {
        if (!_useMockServices) return;

        Logger.LogWarning("Mock mode: using fallback location");
        CurrentLocation = new LocationWithAddress
        {
            Location = new Models.Location { latitude = -17.8292, longitude = 31.0522 },
            FormattedAddress = "Harare, Zimbabwe (Mock)"
        };

        await AnimateToLocation(-17.8292, 31.0522, 15);
    }

    // ═════════════════════════════════════════════════════════════
    //  SEARCH – autocomplete
    // ═════════════════════════════════════════════════════════════

    partial void OnDestinationSearchQueryChanged(string value)
    {
        if (CurrentSearchState != SearchState.PickingDestination) return;
        if (ActiveSearchField != SearchField.Destination) return;
        if (string.IsNullOrWhiteSpace(value))
        {
            Predictions.Clear();
            return;
        }
        _ = DebounceSearchAsync(value);
    }

    partial void OnPickupSearchQueryChanged(string value)
    {
        if (CurrentSearchState != SearchState.PickingDestination) return;
        if (ActiveSearchField != SearchField.Pickup) return;
        if (string.IsNullOrWhiteSpace(value))
        {
            Predictions.Clear();
            return;
        }
        _ = DebounceSearchAsync(value);
    }

    [RelayCommand]
    public void SetActiveField(string fieldName)
    {
        if (Enum.TryParse<SearchField>(fieldName, out var field))
        {
            ActiveSearchField = field;
            Predictions.Clear();
        }
    }

    private async Task DebounceSearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(350, token);
            if (token.IsCancellationRequested) return;
            await SearchPlacesAsync(query, token);
        }
        catch (TaskCanceledException) { /* expected */ }
    }

    private async Task SearchPlacesAsync(string query, CancellationToken ct)
    {
        IsSearching = true;
        Predictions.Clear();

        try
        {
            if (_useMockServices)
            {
                // Return mock predictions so the UI is testable without API keys
                await Task.Delay(200, ct); // simulate network
                Predictions.Add(new PlacePrediction { PlaceId = "mock_1", MainText = "Harare CBD", SecondaryText = "Harare, Zimbabwe", Description = "Harare CBD, Harare, Zimbabwe" });
                Predictions.Add(new PlacePrediction { PlaceId = "mock_2", MainText = "Sam Levy's Village", SecondaryText = "Borrowdale, Harare", Description = "Sam Levy's Village, Borrowdale, Harare" });
                Predictions.Add(new PlacePrediction { PlaceId = "mock_3", MainText = "Eastgate Mall", SecondaryText = "Robert Mugabe Rd, Harare", Description = "Eastgate Mall, Robert Mugabe Rd, Harare" });
                Predictions.Add(new PlacePrediction { PlaceId = "mock_4", MainText = "Avondale Shopping Centre", SecondaryText = "King George Rd, Avondale", Description = "Avondale Shopping Centre, King George Rd, Avondale" });
                return;
            }

            var request = new GoogleApi.Entities.Places.AutoComplete.Request.PlacesAutoCompleteRequest
            {
                Input = query,
                Key = Constants.googlePlacesApiKey,
            };

            // Bias results near the user's current location
            if (CurrentLocation?.Location is not null)
            {
                request.Location = new GoogleApi.Entities.Common.Coordinate(
                    CurrentLocation.Location.latitude,
                    CurrentLocation.Location.longitude);
                request.Radius = 50000; // 50 km
            }

            var response = await _autoCompleteApi.QueryAsync(request);

            if (ct.IsCancellationRequested) return;

            if (response?.Status == GoogleApi.Entities.Common.Enums.Status.Ok && response.Predictions?.Any() == true)
            {
                foreach (var p in response.Predictions)
                {
                    Predictions.Add(new PlacePrediction
                    {
                        PlaceId = p.PlaceId,
                        MainText = p.StructuredFormatting?.MainText ?? p.Description,
                        SecondaryText = p.StructuredFormatting?.SecondaryText ?? string.Empty,
                        Description = p.Description
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            Logger.LogError(ex, "Autocomplete search failed for '{Query}'", query);
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  STATE TRANSITIONS
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    public void OpenSearch()
    {
        // If not logged in and not mock, block
        if (!_useMockServices)
        {
            _ = CheckLoginAndOpenSearch();
            return;
        }
        TransitionToSearch();
    }

    private async Task CheckLoginAndOpenSearch()
    {
        if (!await storageService.IsLoggedInAsync())
        {
            Logger.LogWarning("User not logged in");
            await Shell.Current.DisplayAlert("Log in", "First login in the sidebar", "Ok");
            return;
        }
        TransitionToSearch();
    }

    private void TransitionToSearch()
    {
        DestinationSearchQuery = string.Empty;
        Predictions.Clear();

        // Pre-fill pickup with current GPS location
        PickupLocation = CurrentLocation;
        PickupSearchQuery = CurrentLocation?.FormattedAddress ?? string.Empty;
        ActiveSearchField = SearchField.Destination;

        CurrentSearchState = SearchState.PickingDestination;
    }

    [RelayCommand]
    public async Task SelectPrediction(PlacePrediction prediction)
    {
        if (prediction is null) return;

        Logger.LogInformation("Selected: {Place} for field {Field}", prediction.MainText, ActiveSearchField);
        SelectedPrediction = prediction;
        Predictions.Clear();
        IsSearching = true;

        // Update the correct field's query text
        if (ActiveSearchField == SearchField.Pickup)
            PickupSearchQuery = prediction.MainText;
        else
            DestinationSearchQuery = prediction.MainText;

        try
        {
            // Geocode to get coordinates
            LocationWithAddress dest;

            if (_useMockServices)
            {
                // Mock coordinates for each fake prediction
                var mockCoords = prediction.PlaceId switch
                {
                    "mock_1" => (-17.8294, 31.0539),
                    "mock_2" => (-17.7580, 31.0960),
                    "mock_3" => (-17.8300, 31.0440),
                    "mock_4" => (-17.7950, 31.0400),
                    _ => (-17.8300, 31.0500)
                };

                dest = new LocationWithAddress
                {
                    Location = new Models.Location { latitude = mockCoords.Item1, longitude = mockCoords.Item2 },
                    FormattedAddress = prediction.Description
                };
            }
            else
            {
                // Use address geocode API to forward-geocode the place description
                var geocodeResponse = await _addressGeocodeApi.QueryAsync(
                    new GoogleApi.Entities.Maps.Geocoding.Address.Request.AddressGeocodeRequest
                    {
                        Address = prediction.Description,
                        Key = Constants.googleMapsApiKey
                    });

                var result = geocodeResponse?.Results?.FirstOrDefault();
                if (result is null)
                {
                    Logger.LogWarning("Geocoding returned no results for {Place}", prediction.Description);
                    return;
                }

                dest = new LocationWithAddress
                {
                    Location = new Models.Location
                    {
                        latitude = result.Geometry.Location.Latitude,
                        longitude = result.Geometry.Location.Longitude
                    },
                    FormattedAddress = result.FormattedAddress
                };
            }

            // Assign to the correct target based on active field
            if (ActiveSearchField == SearchField.Pickup)
            {
                PickupLocation = dest;
                // After picking pickup, switch focus to destination
                ActiveSearchField = SearchField.Destination;
                IsSearching = false;
                return;
            }

            SelectedDestination = dest;
            await GetDirectionsAsync();
            CurrentSearchState = SearchState.RoutePreview;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to geocode/route for {Place}", prediction.MainText);

            if (_useMockServices)
            {
                var mockDest = new LocationWithAddress
                {
                    Location = new Models.Location { latitude = -17.83, longitude = 31.05 },
                    FormattedAddress = prediction.Description
                };

                if (ActiveSearchField == SearchField.Pickup)
                {
                    PickupLocation = mockDest;
                    ActiveSearchField = SearchField.Destination;
                    return;
                }

                SelectedDestination = mockDest;
                EstimatedDistanceKm = 8.5;
                EstimatedMinutes = 18;
                RecommendedFare = CalculateRecommendedFare(8.5, 18);
                OfferAmount = RecommendedFare;
                CurrentSearchState = SearchState.RoutePreview;
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        switch (CurrentSearchState)
        {
            case SearchState.PickingDestination:
                ResetSearch();
                break;
            case SearchState.RoutePreview:
                ClearRoute();
                CurrentSearchState = SearchState.PickingDestination;
                break;
            case SearchState.FindingDriver:
                _ = CancelSearch();
                break;
            default:
                break;
        }
    }

    [RelayCommand]
    public void OpenFlyout()
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    /// <summary>
    /// Resets all search/route state back to Idle.
    /// Call when returning from ride flow or when user dismisses.
    /// </summary>
    public void ResetSearch()
    {
        DestinationSearchQuery = string.Empty;
        PickupSearchQuery = string.Empty;
        Predictions.Clear();
        SelectedPrediction = null;
        SelectedDestination = null;
        PickupLocation = null;
        _pendingRideId = null;
        ActiveSearchField = SearchField.Destination;
        ClearRoute();
        CurrentSearchState = SearchState.Idle;
    }

    [RelayCommand]
    public async Task CancelSearch()
    {
        Logger.LogInformation("Cancelling ride search");
        try
        {
            await rideRealtimeService.StopAsync();

            if (!string.IsNullOrEmpty(_pendingRideId))
            {
                await rideApiClient.CancelRide(_pendingRideId);
                Logger.LogInformation("Cancelled ride {RideId}", _pendingRideId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling ride");
        }

        _pendingRideId = null;
        IsBusy = false;
        CurrentSearchState = SearchState.RoutePreview;
    }

    private void ClearRoute()
    {
        Polylines.Clear();
        RoadPolyline = null;
        EstimatedDistanceKm = 0;
        EstimatedMinutes = 0;
        RecommendedFare = 0;
        OfferAmount = 2;
    }

    // ═════════════════════════════════════════════════════════════
    //  ROUTE PREVIEW
    // ═════════════════════════════════════════════════════════════

    private async Task GetDirectionsAsync()
    {
        var origin = PickupLocation ?? CurrentLocation;
        if (origin?.Location is null || SelectedDestination?.Location is null) return;

        Logger.LogInformation("Getting directions from {Origin} to {Dest}", origin.FormattedAddress, SelectedDestination.FormattedAddress);
        IsBusy = true;

        try
        {
            if (_useMockServices)
            {
                await Task.Delay(300);
                EstimatedDistanceKm = 8.5;
                EstimatedMinutes = 18;
                RecommendedFare = CalculateRecommendedFare(EstimatedDistanceKm, EstimatedMinutes);
                OfferAmount = RecommendedFare;

                var startLat = origin.Location.latitude;
                var startLng = origin.Location.longitude;
                var endLat = SelectedDestination.Location.latitude;
                var endLng = SelectedDestination.Location.longitude;

                var points = new PointCollection
                {
                    new Point(startLat, startLng),
                    new Point((startLat + endLat) / 2 + 0.005, (startLng + endLng) / 2 + 0.003),
                    new Point(endLat, endLng)
                };

                RoadPolyline = new Polyline
                {
                    StrokeThickness = 5,
                    StrokeLineJoin = PenLineJoin.Round,
                    Points = points
                };

                Polylines.Clear();
                Polylines.Add(RoadPolyline);

                var bounds = MapUtils.CalculateBounds(points);
                await AnimateToBounds(bounds);
                return;
            }

            var request = new GoogleApi.Entities.Maps.Directions.Request.DirectionsRequest
            {
                Origin = new LocationEx(new CoordinateEx(
                    origin.Location.latitude,
                    origin.Location.longitude)),
                Destination = new LocationEx(new CoordinateEx(
                    SelectedDestination.Location.latitude,
                    SelectedDestination.Location.longitude)),
                Key = Constants.googleMapsApiKey,
                DepartureTime = DateTime.Now,
            };

            var apiResponse = await _directionsApi.QueryAsync(request);

            if (apiResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok))
            {
                var route = apiResponse.Routes.FirstOrDefault();
                if (route is null) return;

                var points = new PointCollection();
                foreach (var coord in route.OverviewPath.Line)
                {
                    points.Add(new Point(coord.Latitude, coord.Longitude));
                }

                RoadPolyline = new Polyline
                {
                    StrokeThickness = 5,
                    StrokeLineJoin = PenLineJoin.Round,
                    Points = points
                };

                Polylines.Clear();
                Polylines.Add(RoadPolyline);

                var leg = route.Legs.FirstOrDefault();
                EstimatedDistanceKm = (leg?.Distance?.Value ?? 0) / 1000d;
                EstimatedMinutes = (int)Math.Ceiling((leg?.Duration?.Value ?? 0) / 60d);
                RecommendedFare = CalculateRecommendedFare(EstimatedDistanceKm, EstimatedMinutes);
                if (OfferAmount <= 0) OfferAmount = RecommendedFare;

                var bounds = MapUtils.GetLatLngBoundsFromViewPort(route.Bounds);
                await AnimateToBounds(bounds);
            }
            else
            {
                Logger.LogWarning("Directions API status: {Status}", apiResponse.Status);
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

    // ═════════════════════════════════════════════════════════════
    //  FIND DRIVER
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task FindDriver()
    {
        var origin = PickupLocation ?? CurrentLocation;
        if (origin?.Location is null || SelectedDestination?.Location is null) return;

        Logger.LogInformation("Finding driver");
        CurrentSearchState = SearchState.FindingDriver;
        IsBusy = true;

        try
        {
            var rideGuid = Guid.NewGuid();
            _pendingRideId = rideGuid.ToString("N");

            var rideRequest = new RideRequestModel
            {
                RideGuid = rideGuid,
                RiderId = await storageService.GetUserIdAsync() ?? string.Empty,
                StartLocation = new Models.Location
                {
                    latitude = origin.Location.latitude,
                    longitude = origin.Location.longitude
                },
                DestinationLocation = new Models.Location
                {
                    latitude = SelectedDestination.Location.latitude,
                    longitude = SelectedDestination.Location.longitude
                },
                OfferAmount = OfferAmount,
                Comments = "Nothing entered"
            };

            Logger.LogInformation("Ride request ID: {Id}", rideRequest.RideGuid);

            var response = await rideApiClient.RequestRide(rideRequest);

            if (response.IsSuccess)
            {
                Logger.LogInformation("Ride request sent — navigating to selection");
                await rideRealtimeService.StartRiderMatchingAsync(rideRequest);

                await Shell.Current.GoToAsync(nameof(RideSelectionPage), true,
                    new Dictionary<string, object>
                    {
                        { "rideRequest", rideRequest }
                    });

                ResetSearch();
            }
            else
            {
                Logger.LogWarning("Ride request failed: {Error}", response.ErrorMessage);
                CurrentSearchState = SearchState.RoutePreview;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error requesting ride");
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            CurrentSearchState = SearchState.RoutePreview;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  OFFER ADJUSTMENT
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    public void IncreaseFare()
    {
        OfferAmount += 0.50m;
    }

    [RelayCommand]
    public void DecreaseFare()
    {
        if (OfferAmount > 0.50m)
            OfferAmount -= 0.50m;
    }

    // ═════════════════════════════════════════════════════════════
    //  CAMERA HELPERS
    // ═════════════════════════════════════════════════════════════

    private async Task AnimateToLocation(double lat, double lng, float zoom)
    {
        if (AnimateCameraFunc is null) return;
        var update = CameraUpdateFactory.NewLatLngZoom(new(lat, lng), zoom);
        await AnimateCameraFunc(update, 2000);
    }

    private async Task AnimateToBounds(LatLngBounds bounds)
    {
        if (AnimateCameraFunc is null) return;
        // Use larger padding since the map shares space with the bottom sheet
        var update = CameraUpdateFactory.NewLatLngBounds(bounds, 100);
        await AnimateCameraFunc(update, 2000);
    }

    // ═════════════════════════════════════════════════════════════
    //  FARE CALCULATION
    // ═════════════════════════════════════════════════════════════

    private static decimal CalculateRecommendedFare(double distanceKm, int etaMinutes)
    {
        var baseFare = 1.50m;
        var distanceComponent = (decimal)distanceKm * 0.75m;
        var durationComponent = etaMinutes * 0.09m;
        var hour = DateTime.Now.Hour;
        var multiplier = hour is >= 17 and <= 20 ? 1.15m : 1m;
        return decimal.Round((baseFare + distanceComponent + durationComponent) * multiplier, 2);
    }
}
