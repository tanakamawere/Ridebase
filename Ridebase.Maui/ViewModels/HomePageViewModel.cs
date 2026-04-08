using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
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

    // ─── Map bindings (Refactored for Mapsui) ─────────────────────
    // Logic now handled via OnRequestMapUpdate event

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
    // Handle route display via OnRequestMapUpdate or simplified models

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
    private readonly IMapService _mapService;
    private readonly ILocationService _locationService;
    private CancellationTokenSource? _searchCts;
    private string? _pendingRideId;

    public HomePageViewModel(
        IPopupNavigation navigation,
        IMapService mapService,
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

        _mapService = mapService;
        _locationService = locationService;

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
            Logger.LogInformation("Attempting to get current location...");
            IsBusy = true;
            LocationStatusMessage = "Acquiring GPS fix...";

            var result = await _locationService.GetCurrentLocationAsync();
            LocationPermissionGranted = result.Status == LocationAcquisitionStatus.Success;

            if (result.Status != LocationAcquisitionStatus.Success || result.DeviceLocation is null)
            {
                Logger.LogWarning("Location acquisition failed: {Status}. {Message}. Using Harare as fallback.", result.Status, result.ErrorMessage);
                
                // Fallback: Start in Harare instead of showing an empty world map
                var fallbackLat = -17.8248;
                var fallbackLng = 31.0530;
                
                CurrentLocation = await ReverseGeocodeAsync(fallbackLat, fallbackLng);
                PickupLocation ??= CurrentLocation;
                PickupSearchQuery = CurrentLocation.FormattedAddress;
                
                HasLocationError = true;
                LocationStatusMessage = "GPS unavailable. Using default (Harare).";
                
                await AnimateToLocation(fallbackLat, fallbackLng, 15);
                return;
            }

            Logger.LogInformation("GPS success: {Lat}, {Lng}", result.DeviceLocation.Latitude, result.DeviceLocation.Longitude);
            LocationStatusMessage = "Reverse geocoding...";

            CurrentLocation = await ReverseGeocodeAsync(result.DeviceLocation.Latitude, result.DeviceLocation.Longitude);
            PickupLocation ??= CurrentLocation;

            if (string.IsNullOrWhiteSpace(PickupSearchQuery))
            {
                PickupSearchQuery = CurrentLocation.FormattedAddress;
            }

            HasLocationError = false;
            LocationStatusMessage = "Location ready";

            Logger.LogInformation("Location ready: {Address}", CurrentLocation.FormattedAddress);
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
            var results = await _mapService.GetAutocompleteAsync(query);

            if (ct.IsCancellationRequested) return;

            if (results?.Any() == true)
            {
                foreach (var p in results)
                {
                    Predictions.Add(p);
                }

                SearchFeedbackMessage = "Select a place";
                return;
            }

            SearchFeedbackMessage = "No places found. Try a different search.";
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            Logger.LogError(ex, "Autocomplete search failed for '{Query}'", query);
            SearchFeedbackMessage = "Search failed. Check your network or choose the location on the map.";
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
            var details = await _mapService.GetPlaceDetailsAsync(prediction.PlaceId);
            if (details.Latitude == 0 && details.Longitude == 0)
            {
                Logger.LogWarning("Geocoding returned no results for {Place}", prediction.Description);
                SearchFeedbackMessage = "Could not find coordinates for this place.";
                return;
            }

            var dest = new LocationWithAddress
            {
                FormattedAddress = details.Address,
                Location = new Models.Location { latitude = details.Latitude, longitude = details.Longitude }
            };

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

    public async Task HandleMapSelectionCallback(double latitude, double longitude)
    {
        var loc = await ReverseGeocodeAsync(latitude, longitude);
        if (loc == null) return;

        if (ActiveSearchField == SearchField.Pickup)
        {
            PickupLocation = loc;
            PickupSearchQuery = loc.FormattedAddress;
        }
        else
        {
            SelectedDestination = loc;
            DestinationSearchQuery = loc.FormattedAddress;
        }

        if (PickupLocation != null && SelectedDestination != null)
        {
            await GetDirectionsAsync();
            CurrentSearchState = SearchState.RoutePreview;
        }
        else
        {
            CurrentSearchState = SearchState.PickingDestination;
        }
    }

    public async Task<LocationWithAddress> ReverseGeocodeAsync(double lat, double lng)
    {
        var details = await _mapService.ReverseGeocodeAsync(lat, lng);
        return new LocationWithAddress
        {
            FormattedAddress = details.Address,
            Location = new Models.Location { latitude = details.Latitude, longitude = details.Longitude }
        };
    }

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

    public async Task AnimateToBounds(params (double lat, double lng)[] points)
    {
       // Implementation omitted for brevity, logic will be in HomePage.xaml.cs
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
            var routeInfo = await _mapService.GetDirectionsAsync(
                origin.Location.latitude,
                origin.Location.longitude,
                SelectedDestination.Location.latitude,
                SelectedDestination.Location.longitude);

            if (routeInfo != null)
            {
                // We'll handle drawing the polyline in the Code-Behind or via a Mapsui helper
                // For now, we update the ViewModel state
                
                EstimatedDistanceKm = routeInfo.DistanceKm;
                EstimatedMinutes = (int)Math.Ceiling(routeInfo.DurationMinutes);
                RecommendedFare = CalculateRecommendedFare(EstimatedDistanceKm, EstimatedMinutes);
                if (OfferAmount <= 0) OfferAmount = RecommendedFare;

                // Signal to the view to update the map
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

    public void RequestMapUpdate(MapUpdateType type, double lat = 0, double lon = 0, double zoom = 0, string? polyline = null)
    {
        OnRequestMapUpdate?.Invoke(this, new MapUpdateEventArgs 
        { 
            Type = type, 
            Latitude = lat, 
            Longitude = lon, 
            Zoom = zoom,
            RoutePolyline = polyline
        });
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
        
        // Use OSM Geocomplete/Geocode
        var results = await _mapService.GetAutocompleteAsync(address);
        var first = results?.FirstOrDefault();
        if (first == null)
        {
            SearchFeedbackMessage = "Could not find that location.";
            return;
        }

        var details = await _mapService.GetPlaceDetailsAsync(first.PlaceId);
        
        SelectedDestination = new LocationWithAddress
        {
            FormattedAddress = details.Address,
            Location = new Models.Location { latitude = details.Latitude, longitude = details.Longitude }
        };

        await GetDirectionsAsync();
        CurrentSearchState = SearchState.RoutePreview;
    }

    private LocationWithAddress? GetActiveFieldLocationForMapSelection()
    {
        return ActiveSearchField == SearchField.Pickup
            ? PickupLocation ?? CurrentLocation
            : SelectedDestination ?? CurrentLocation ?? PickupLocation;
    }
}
