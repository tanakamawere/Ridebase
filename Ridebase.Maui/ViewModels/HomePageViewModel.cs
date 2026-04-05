using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
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

    [ObservableProperty]
    private string locationStatusMessage = "Finding your location...";

    [ObservableProperty]
    private bool hasLocationError;

    // ─── Map bindings ────────────────────────────────────────────
    [ObservableProperty]
    private CameraUpdate? _initialCameraPosition;

    [ObservableProperty]
    private Action<CameraUpdate>? _moveCameraAction;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task>? _animateCameraFunc;

    [ObservableProperty]
    private CameraPosition? currentCameraPosition;

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

    [ObservableProperty]
    private string mapSelectionPrompt = "Center the pin over the exact destination.";

    [ObservableProperty]
    private string mapSelectionHelperText = "Move the map until the marker is on the exact point, then confirm.";

    [ObservableProperty]
    private string searchFeedbackMessage = "Type to search for a place";

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

    [ObservableProperty]
    private string findingDriverMessage = "Broadcasting your request to nearby drivers.";

    // ─── Services ────────────────────────────────────────────────
    private readonly GoogleMaps.Geocode.LocationGeocodeApi _geocodeApi;
    private readonly GoogleMaps.Geocode.AddressGeocodeApi _addressGeocodeApi;
    private readonly GooglePlaces.AutoCompleteApi _autoCompleteApi;
    private readonly GoogleMaps.DirectionsApi _directionsApi;
    private readonly ILocationService _locationService;
    private readonly string _googleMapsApiKey;
    private readonly string _googlePlacesApiKey;
    private CancellationTokenSource? _searchCts;
    private string? _pendingRideId;

    public HomePageViewModel(
        IPopupNavigation navigation,
        GoogleMaps.Geocode.LocationGeocodeApi locationGeocodeApi,
        GoogleMaps.Geocode.AddressGeocodeApi addressGeocodeApi,
        GooglePlaces.AutoCompleteApi autoCompleteApi,
        GoogleMaps.DirectionsApi directionsApi,
        ILocationService locationService,
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
        _locationService = locationService;
        _googleMapsApiKey = configuration["GoogleKeys:MapsApiKey"] ?? string.Empty;
        _googlePlacesApiKey = configuration["GoogleKeys:PlacesApiKey"] ?? string.Empty;

        popupNavigation = navigation;
        storageService = storage;
        rideApiClient = rideApi;
        rideRealtimeService = realtimeService;
        rideStateStore = rideStore;
        userSessionService = userSession;

        _ = GetCurrentLocation();
    }

    public string CurrentLocationHeadline => string.IsNullOrWhiteSpace(CurrentLocation?.FormattedAddress)
        ? LocationStatusMessage
        : CurrentLocation.FormattedAddress;

    partial void OnCurrentLocationChanged(LocationWithAddress? value)
    {
        OnPropertyChanged(nameof(CurrentLocationHeadline));
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

            var result = await _locationService.GetCurrentLocationAsync();
            LocationPermissionGranted = result.Status == LocationAcquisitionStatus.Success;

            if (result.Status != LocationAcquisitionStatus.Success || result.DeviceLocation is null)
            {
                HasLocationError = true;
                LocationStatusMessage = result.Status switch
                {
                    LocationAcquisitionStatus.PermissionDenied => "Location permission is off. Set pickup manually or choose a point on the map.",
                    LocationAcquisitionStatus.NotSupported => "This device doesn't support location. Set pickup manually or choose a point on the map.",
                    LocationAcquisitionStatus.Unavailable => "Current location unavailable right now. Set pickup manually or choose a point on the map.",
                    _ => "We couldn't fetch your current location. Set pickup manually or choose a point on the map."
                };

                Logger.LogWarning("Unable to get current location. Status: {Status}. Message: {Message}", result.Status, result.ErrorMessage);
                return;
            }

            Logger.LogInformation("Location: {Lat}, {Lng}", result.DeviceLocation.Latitude, result.DeviceLocation.Longitude);

            CurrentLocation = await ReverseGeocodeAsync(result.DeviceLocation.Latitude, result.DeviceLocation.Longitude);
            PickupLocation ??= CurrentLocation;

            if (string.IsNullOrWhiteSpace(PickupSearchQuery))
            {
                PickupSearchQuery = CurrentLocation.FormattedAddress;
            }

            HasLocationError = false;
            LocationStatusMessage = "Current location ready";

            Logger.LogInformation("Current location: {Address}", CurrentLocation.FormattedAddress);
            await AnimateToLocation(CurrentLocation.Location.latitude, CurrentLocation.Location.longitude, 15);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current location");
            HasLocationError = true;
            LocationStatusMessage = "Unable to fetch your current location. Set pickup manually or choose a point on the map.";
        }
        finally
        {
            IsBusy = false;
        }
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
            SearchFeedbackMessage = "Type to search for a place";
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
            SearchFeedbackMessage = "Type to search for a place";
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
        SearchFeedbackMessage = "Searching places...";

        try
        {
            if (string.IsNullOrWhiteSpace(_googlePlacesApiKey))
            {
                SearchFeedbackMessage = "Google Places API key is missing. Add it in appsettings.json.";
                Logger.LogWarning("Google Places API key is missing.");
                return;
            }

            var request = new GoogleApi.Entities.Places.AutoComplete.Request.PlacesAutoCompleteRequest
            {
                Input = query,
                Key = _googlePlacesApiKey,
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

                SearchFeedbackMessage = "Select a place";
                return;
            }

            Logger.LogWarning("Autocomplete returned status {Status} for '{Query}'", response?.Status, query);

            var fallbackResult = await GeocodeAddressAsync(query);
            if (fallbackResult.Location is not null)
            {
                Predictions.Add(new PlacePrediction
                {
                    PlaceId = query,
                    MainText = fallbackResult.Location.FormattedAddress,
                    SecondaryText = "Resolved from typed address",
                    Description = fallbackResult.Location.FormattedAddress
                });

                SearchFeedbackMessage = "Select the matched address";
                return;
            }

            SearchFeedbackMessage = BuildSearchFailureMessage(response is null ? null : response.Status.ToString(), fallbackResult.ErrorMessage);
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            Logger.LogError(ex, "Autocomplete search failed for '{Query}'", query);
            SearchFeedbackMessage = "Search failed. Check your Google API setup or choose the location on the map.";
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
        TransitionToSearch();
    }

    [RelayCommand]
    public async Task UsePresetDestination(string destinationLabel)
    {
        TransitionToSearch();
        await SetDestinationFromAddressAsync(destinationLabel);
    }

    private async Task CheckLoginAndOpenSearch()
    {
        if (!await storageService.IsLoggedInAsync())
        {
            Logger.LogWarning("User not logged in");
            await Shell.Current.DisplayAlertAsync("Log in", "First login in the sidebar", "Ok");
            return;
        }
        TransitionToSearch();
    }

    private void TransitionToSearch()
    {
        DestinationSearchQuery = string.Empty;
        Predictions.Clear();
        SelectedDestination = null;
        ClearRoute();
        SearchFeedbackMessage = "Type to search for a place";

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
            var geocodeResult = await GeocodeAddressAsync(prediction.Description);
            if (geocodeResult.Location is null)
            {
                Logger.LogWarning("Geocoding returned no results for {Place}", prediction.Description);
                SearchFeedbackMessage = BuildSearchFailureMessage(null, geocodeResult.ErrorMessage);
                return;
            }

            var dest = geocodeResult.Location;

            // Assign to the correct target based on active field
            if (ActiveSearchField == SearchField.Pickup)
            {
                PickupLocation = dest;
                PickupSearchQuery = dest.FormattedAddress;

                if (SelectedDestination?.Location is not null)
                {
                    await GetDirectionsAsync();
                    CurrentSearchState = SearchState.RoutePreview;
                    return;
                }

                // After picking pickup, switch focus to destination
                ActiveSearchField = SearchField.Destination;
                IsSearching = false;
                return;
            }

            SelectedDestination = dest;
            DestinationSearchQuery = dest.FormattedAddress;
            await GetDirectionsAsync();
            CurrentSearchState = SearchState.RoutePreview;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to geocode/route for {Place}", prediction.MainText);
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
            case SearchState.PinningLocation:
                Predictions.Clear();
                CurrentSearchState = SearchState.PickingDestination;
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

    [RelayCommand]
    public async Task UseCurrentLocationForPickup()
    {
        await GetCurrentLocation();
        if (CurrentLocation is null)
        {
            return;
        }

        PickupLocation = CurrentLocation;
        PickupSearchQuery = CurrentLocation.FormattedAddress;

        if (SelectedDestination?.Location is not null)
        {
            await GetDirectionsAsync();
            CurrentSearchState = SearchState.RoutePreview;
            return;
        }

        ActiveSearchField = SearchField.Destination;
        CurrentSearchState = SearchState.PickingDestination;
    }

    [RelayCommand]
    public async Task ChooseLocationOnMap(object? fieldName)
    {
        if (!Enum.TryParse<SearchField>(fieldName?.ToString(), out var field))
        {
            return;
        }

        ActiveSearchField = field;
        Predictions.Clear();
        IsSearching = false;
        _searchCts?.Cancel();

        MapSelectionPrompt = field == SearchField.Pickup
            ? "Center the pin over the pickup spot."
            : "Center the pin over the destination.";
        MapSelectionHelperText = field == SearchField.Pickup
            ? "Drag the map until the pickup pin is exact, then confirm."
            : "Drag the map until the destination pin is exact, then confirm.";

        var focusLocation = GetActiveFieldLocationForMapSelection();
        if (focusLocation?.Location is not null)
        {
            await AnimateToLocation(focusLocation.Location.latitude, focusLocation.Location.longitude, 16);
        }

        CurrentSearchState = SearchState.PinningLocation;
    }

    [RelayCommand]
    public async Task ConfirmMapSelection()
    {
        var target = CurrentCameraPosition?.Target;
        if (!target.HasValue)
        {
            await Shell.Current.DisplayAlertAsync("Map selection", "Move the map to the exact point before confirming.", "OK");
            return;
        }

        var selectedLocation = await ReverseGeocodeAsync(target.Value.X, target.Value.Y);
        if (ActiveSearchField == SearchField.Pickup)
        {
            PickupLocation = selectedLocation;
            PickupSearchQuery = selectedLocation.FormattedAddress;

            if (SelectedDestination?.Location is not null)
            {
                await GetDirectionsAsync();
                CurrentSearchState = SearchState.RoutePreview;
                return;
            }

            ActiveSearchField = SearchField.Destination;
            CurrentSearchState = SearchState.PickingDestination;
            return;
        }

        SelectedDestination = selectedLocation;
        DestinationSearchQuery = selectedLocation.FormattedAddress;
        await GetDirectionsAsync();
        CurrentSearchState = SearchState.RoutePreview;
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

        if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            await Shell.Current.DisplayAlertAsync("Google Maps", "Google Maps API key is missing. Add it in appsettings.json.", "OK");
            return;
        }

        Logger.LogInformation("Getting directions from {Origin} to {Dest}", origin.FormattedAddress, SelectedDestination.FormattedAddress);
        IsBusy = true;

        try
        {
            var request = new GoogleApi.Entities.Maps.Directions.Request.DirectionsRequest
            {
                Origin = new LocationEx(new CoordinateEx(
                    origin.Location.latitude,
                    origin.Location.longitude)),
                Destination = new LocationEx(new CoordinateEx(
                    SelectedDestination.Location.latitude,
                    SelectedDestination.Location.longitude)),
                Key = _googleMapsApiKey,
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
        if (origin?.Location is null || SelectedDestination?.Location is null)
        {
            await Shell.Current.DisplayAlertAsync("Missing locations", "Choose both pickup and destination before finding offers.", "OK");
            return;
        }

        Logger.LogInformation("Finding driver");
        CurrentSearchState = SearchState.FindingDriver;
        IsBusy = true;
        FindingDriverMessage = "Broadcasting your request to nearby drivers.";

        try
        {
            var rideGuid = Guid.NewGuid();
            _pendingRideId = rideGuid.ToString("N");
            var session = await userSessionService.GetStateAsync();

            var rideRequest = new RideRequestModel
            {
                RideGuid = rideGuid,
                RiderId = await storageService.GetUserIdAsync() ?? string.Empty,
                RiderName = string.IsNullOrWhiteSpace(session.FullName) ? "Kinetic Rider" : session.FullName,
                RiderPhoneNumber = session.PhoneNumber,
                StartLocation = new Models.Location
                {
                    latitude = origin.Location.latitude,
                    longitude = origin.Location.longitude
                },
                StartAddress = origin.FormattedAddress ?? "Pickup location",
                DestinationLocation = new Models.Location
                {
                    latitude = SelectedDestination.Location.latitude,
                    longitude = SelectedDestination.Location.longitude
                },
                DestinationAddress = SelectedDestination.FormattedAddress ?? "Destination",
                OfferAmount = OfferAmount,
                RecommendedAmount = RecommendedFare,
                EstimatedDistanceKm = EstimatedDistanceKm,
                EstimatedMinutes = EstimatedMinutes,
                RequestedAtUtc = DateTimeOffset.UtcNow,
                Comments = "Please call on arrival."
            };

            Logger.LogInformation("Ride request ID: {Id}", rideRequest.RideGuid);

            var response = await rideApiClient.RequestRide(rideRequest);

            if (response.IsSuccess)
            {
                Logger.LogInformation("Ride request sent — navigating to selection");
                FindingDriverMessage = "Drivers can now accept or counter your offer.";
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
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
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

    private async Task SetDestinationFromAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        DestinationSearchQuery = address;
        ActiveSearchField = SearchField.Destination;
        var destination = await GeocodeAddressAsync(address);
        if (destination.Location is null)
        {
            SearchFeedbackMessage = BuildSearchFailureMessage(null, destination.ErrorMessage);
            return;
        }

        SelectedDestination = destination.Location;
        await GetDirectionsAsync();
        CurrentSearchState = SearchState.RoutePreview;
    }

    private LocationWithAddress? GetActiveFieldLocationForMapSelection()
    {
        return ActiveSearchField == SearchField.Pickup
            ? PickupLocation ?? CurrentLocation
            : SelectedDestination ?? CurrentLocation ?? PickupLocation;
    }

    private async Task<LocationWithAddress> ReverseGeocodeAsync(double latitude, double longitude)
    {
        var fallbackAddress = $"{latitude:F5}, {longitude:F5}";

        try
        {
            if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
            {
                Logger.LogWarning("Google Maps API key is missing for reverse geocoding.");
                return new LocationWithAddress
                {
                    Location = new Models.Location
                    {
                        latitude = latitude,
                        longitude = longitude
                    },
                    FormattedAddress = fallbackAddress
                };
            }

            var locationGeocodeRequest = new LocationGeocodeRequest
            {
                Location = new Coordinate(latitude, longitude),
                Key = _googleMapsApiKey
            };

            var response = await _geocodeApi.QueryAsync(locationGeocodeRequest);
            var firstResult = response?.Results?.FirstOrDefault();
            if (response?.Status != GoogleApi.Entities.Common.Enums.Status.Ok || firstResult is null)
            {
                Logger.LogWarning("Reverse geocode failed for {Lat}, {Lng}. Status: {Status}", latitude, longitude, response?.Status);
                return new LocationWithAddress
                {
                    Location = new Models.Location
                    {
                        latitude = latitude,
                        longitude = longitude
                    },
                    FormattedAddress = fallbackAddress
                };
            }

            return new LocationWithAddress
            {
                Location = new Models.Location
                {
                    latitude = latitude,
                    longitude = longitude
                },
                FormattedAddress = firstResult.FormattedAddress ?? fallbackAddress
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Reverse geocode failed for {Lat}, {Lng}", latitude, longitude);
            return new LocationWithAddress
            {
                Location = new Models.Location
                {
                    latitude = latitude,
                    longitude = longitude
                },
                FormattedAddress = fallbackAddress
            };
        }
    }

    private async Task<(LocationWithAddress? Location, string? ErrorMessage)> GeocodeAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            Logger.LogWarning("Google Maps API key is missing.");
            return (null, "Google Maps API key is missing. Add it in appsettings.json.");
        }

        var geocodeResponse = await _addressGeocodeApi.QueryAsync(
            new GoogleApi.Entities.Maps.Geocoding.Address.Request.AddressGeocodeRequest
            {
                Address = address,
                Key = _googleMapsApiKey
            });

        var result = geocodeResponse?.Results?.FirstOrDefault();
        if (result is null)
        {
            Logger.LogWarning("Forward geocode returned status {Status} for '{Address}'", geocodeResponse?.Status, address);
            return (null, BuildSearchFailureMessage(geocodeResponse is null ? null : geocodeResponse.Status.ToString(), null));
        }

        return (new LocationWithAddress
        {
            Location = new Models.Location
            {
                latitude = result.Geometry.Location.Latitude,
                longitude = result.Geometry.Location.Longitude
            },
            FormattedAddress = result.FormattedAddress
        }, null);
    }

    private string BuildSearchFailureMessage(string? status, string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
        {
            return detail;
        }

        return status switch
        {
            "RequestDenied" or "REQUEST_DENIED" => "Google rejected the search request. Check the API key, billing, and app restrictions.",
            "OverQueryLimit" or "OVER_QUERY_LIMIT" => "Google search quota has been reached. Try again later or check quota limits.",
            "InvalidRequest" or "INVALID_REQUEST" => "Google search request was invalid. Try a fuller place name or choose on map.",
            "ZeroResults" or "ZERO_RESULTS" => "No exact match found. Try 'Joina City Mall, Harare' or choose on map.",
            _ => "We couldn't find that place. Try the full place name or choose on map."
        };
    }
}
