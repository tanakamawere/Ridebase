using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;
using Ridebase.Services.RideService;

namespace Ridebase.ViewModels;

public partial class MapHomeViewModel : BaseViewModel
{
    [ObservableProperty]
    private string searchQuery;
    [ObservableProperty]
    private decimal rideAmount;

    //The following obviously defaults to the current location, but can be changed by the user
    [ObservableProperty]
    private string startLocation;

    [ObservableProperty]
    private List<Place> placesList = new();

    //Destination location/place more strictly
    [ObservableProperty]
    private Place destinationPlace = new();
    //Start place
    [ObservableProperty]
    private Place startPlace = new();
    [ObservableProperty]
    private bool isLocationSheetVisible = true;

    //Create ride root and add properties to it to send to the backend
    public RideRoot rideRoot { get; set; } = new();

    

    public MapHomeViewModel(IPlaces places, IGeocodeGoogle geocodeGoogle, IPopupNavigation navigation)
    {
        Title = "Map Page";
        placesApi = places;
        this.geocodeGoogle = geocodeGoogle;
        popupNavigation = navigation;
    }

    //Search for places using Google Places API
    //Location type here is for telling the collection view which location to update, and to append "Current Location" option to the list
    public async Task SearchPlaces(string keyword, LocationType locationType = LocationType.Destination)
    {
        IsBusy = true;
        PlacesList.Clear();
        try
        {
            PlacesList = await placesApi.GetPlacesAutocomplete(keyword);
            if (locationType.Equals(LocationType.Start))
            {
                LocationWithAddress locationWithAddress = await geocodeGoogle.GetCurrentLocationWithAddressAsync();
                PlacesList.Add(new Place
                {
                    displayName = new DisplayName
                    {
                        text = "Current Location"
                    },
                    formattedAddress = locationWithAddress.FormattedAddress,
                    location = locationWithAddress.Location
                });
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

    [RelayCommand]
    partial void OnSearchQueryChanged(string value)
    {
        //Check if value is not null
        if (!string.IsNullOrEmpty(value))
            SearchPlaces(value);
    }

    //When startLocation is changed, update the collection view as well
    [RelayCommand]
    partial void OnStartLocationChanged(string value)
    {
        if(!string.IsNullOrEmpty(value))
            SearchPlaces(value);
    }

    //On click of a place, set the destination place
    public void SelectDestinationPlace(Place place)
    {
        if (place != null)
        {
            if (place.displayName.text == "Current Location")
            {
                StartPlace = place;
            }
            else
            {
                DestinationPlace = place;
            }
        }
    }

    //Relay command to add or subtract the ride amount depending on the command parameter
    [RelayCommand]
    public void CalculateRideAmount(string operant)
    {
        if (operant.Equals("Add"))
        {
            RideAmount += 1;
        }
        else if (operant.Equals("Subtract"))
        {
            RideAmount -= 1;
        }
    }
}

public enum LocationType
{
    Start,
    Destination
}
