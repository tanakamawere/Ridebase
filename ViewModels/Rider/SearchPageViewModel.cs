using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Places.Common;
using Microsoft.IdentityModel.Tokens;
using MPowerKit.GoogleMaps;
using Ridebase.Models;
using Ridebase.Pages.Rider;
using Ridebase.Services.Geocoding;
using System.Collections.ObjectModel;
using Location = Ridebase.Models.Location;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(CurrentLocation), "currentLocation")]
public partial class SearchPageViewModel : BaseViewModel
{
    [ObservableProperty]
    private LocationWithAddress currentLocation;
    //Start place
    [ObservableProperty]
    private PlaceResult startPlace;
    [ObservableProperty]
    private PlaceResult destinationPlace;
    [ObservableProperty]
    private string startLocation;
    [ObservableProperty]
    private string startSearchQuery;
    [ObservableProperty]
    private string destinationSearchQuery;
    [ObservableProperty]
    public ObservableCollection<PlaceResult> places = [];
    //Used for determining which place, start or destination is to be set
    private LocationType locationType = LocationType.Destination;

    private readonly GooglePlaces.Search.NearBySearchApi nearBySearchApi;

    public SearchPageViewModel(GooglePlaces.Search.NearBySearchApi _googlePlaces)
    {
        Title = "Search";
        nearBySearchApi = _googlePlaces;
        //Convert the current location to a place object when navigated 
    }

    public async Task SearchPlaces(string keyword)
    {
        IsBusy = true;
        Places.Clear();
        try
        {
            var nearbySearchResponse = await nearBySearchApi.QueryAsync(new GoogleApi.Entities.Places.Search.NearBy.Request.PlacesNearBySearchRequest
            {
                Location = new GoogleApi.Entities.Common.Coordinate(CurrentLocation.Location.latitude, CurrentLocation.Location.longitude),
                Radius = 50000,
                Key = "AIzaSyArmqo-1_M4O-UoP08k339M6wHN8-AAPa8",
                Keyword = keyword
            });

            if (nearbySearchResponse.Status.Equals(GoogleApi.Entities.Common.Enums.Status.Ok))
            {
                foreach (var item in nearbySearchResponse.Results)
                {
                    Places.Add(item);
                }
            }
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

    partial void OnDestinationSearchQueryChanged(string value)
    {
        //Check if value is not null
        if (!string.IsNullOrEmpty(value))
            SearchPlaces(value);
    }

    partial void OnStartSearchQueryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            SearchPlaces(value);
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
    public void SelectPlace(PlaceResult place)
    {
        if (place == null) return;

        switch (locationType)
        {
            case LocationType.Start:
                StartPlace = place;
                StartSearchQuery = place.Name;
                break;

            case LocationType.Destination:
                DestinationPlace = place;
                DestinationSearchQuery = place.Name;
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
}

public enum LocationType
{
    Start,
    Destination
}