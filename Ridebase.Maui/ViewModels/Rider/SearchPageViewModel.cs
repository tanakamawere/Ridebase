using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Places.Common;
using GoogleApi.Entities.Places.Search.NearBy.Request;
using Microsoft.Extensions.Logging;
using Ridebase.Pages;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(CurrentLocation), "currentLocation")]
public partial class SearchPageViewModel : BaseViewModel
{
    [ObservableProperty]
    private LocationWithAddress? currentLocation;
    //Start place
    [ObservableProperty]
    private PlaceResult? startPlace;
    [ObservableProperty]
    private PlaceResult? destinationPlace;
    [ObservableProperty]
    private string startLocation = string.Empty;
    [ObservableProperty]
    private string startSearchQuery = string.Empty;
    [ObservableProperty]
    private string destinationSearchQuery = string.Empty;
    [ObservableProperty]
    public ObservableCollection<PlaceResult> places = [];
    //Used for determining which place, start or destination is to be set
    private LocationType locationType = LocationType.Destination;
    private CancellationTokenSource? cts;

    private readonly GooglePlaces.Search.NearBySearchApi nearBySearchApi;

    public SearchPageViewModel(GooglePlaces.Search.NearBySearchApi _googlePlaces, ILogger<SearchPageViewModel> logger)
    {
        Title = "Search";
        Logger = logger;
        nearBySearchApi = _googlePlaces;
    }

    public async Task SearchPlaces(string keyword)
    {
        Logger.LogInformation("Searching for places with keyword: {Keyword}", keyword);
        IsBusy = true;
        Places.Clear();
        try
        {
            PlacesNearBySearchRequest placesNearBySearchRequest = new ()
            {
                Location = new GoogleApi.Entities.Common.Coordinate(CurrentLocation.Location.latitude, CurrentLocation.Location.longitude),
                Radius = 50000,
                Key = "AIzaSyArmqo-1_M4O-UoP08k339M6wHN8-AAPa8",
                Keyword = keyword
            };

            var nearbySearchResponse = await nearBySearchApi.QueryAsync(placesNearBySearchRequest);

            string something = nearbySearchResponse.ErrorMessage;

            if (nearbySearchResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok))
            {
                Logger.LogInformation("Found {Count} places for keyword: {Keyword}", nearbySearchResponse.Results.Count(), keyword);
                foreach (var item in nearbySearchResponse.Results)
                {
                    Places.Add(item);
                }
            }
            else
            {
                Logger.LogWarning("Places search returned status: {Status}, Error: {ErrorMessage}", nearbySearchResponse.Status, nearbySearchResponse.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching for places with keyword: {Keyword}", keyword);
            //Display error message
            await AppShell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnDestinationSearchQueryChanged(string value)
    {
        //Check if value is not null
        if (!string.IsNullOrEmpty(value))
            DebounceSearch(value);
    }

    partial void OnStartSearchQueryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            DebounceSearch(value);
    }

    private async Task DebounceSearch(string query)
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();

        await Task.Delay(00, cts.Token);

        if (cts.IsCancellationRequested)
            return;

        await SearchPlaces(query);
    }

    [RelayCommand]
    //navigate to page to choose location on map
    public async Task ChooseLocationOnMap()
    {
        //TODO: Change to a map page and handle this
        await Shell.Current.GoToAsync(nameof(RideDetailsPage));
    }

    [RelayCommand]
    //Method to select the location from the collection view
    public async void SelectPlace(PlaceResult place)
    {
        if (place == null)
        {
            Logger.LogWarning("SelectPlace called with null place");
            return;
        }

        Logger.LogInformation("Place selected: {PlaceName}, Type: {LocationType}", place.Name, locationType);

        switch (locationType)
        {
            case LocationType.Start:
                StartPlace = place;
                StartSearchQuery = place.Name;
                Logger.LogInformation("Start location set to: {PlaceName}", place.Name);
                break;

            case LocationType.Destination:

                DestinationPlace = place;
                DestinationSearchQuery = place.Name;
                Logger.LogInformation("Destination location set to: {PlaceName}", place.Name);
                //meaning if the user has already selected the start location
                if (!StartPlace.Equals(null))
                {
                    await GoToRideDetailsPage();
                }
                break;
        }
        Places.Clear();
    }

    [RelayCommand]
    public void EntryFocused(LocationType _locationType)
    {
        Places.Clear();
        locationType = _locationType;
    }

    //Method to go to the ride details page with the start and destination locations
    public async Task GoToRideDetailsPage()
    {
        if (StartPlace == null || DestinationPlace == null)
        {
            Logger.LogWarning("Cannot navigate to RideDetailsPage: StartPlace or DestinationPlace is null");
            await AppShell.Current.DisplayAlertAsync("Error", "Please select both start and destination locations", "OK");
            return;
        }

        Logger.LogInformation("Navigating to RideDetailsPage with Start: {StartPlace} and Destination: {DestinationPlace}", StartPlace.Name, DestinationPlace.Name);
        await Shell.Current.GoToAsync(nameof(RideDetailsPage), true, new Dictionary<string, object>
        {
            {"startPlace", StartPlace },
            {"destinationPlace", DestinationPlace }
        });
    }
}

public enum LocationType
{
    Start,
    Destination
}