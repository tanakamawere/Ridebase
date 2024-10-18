using Maui.GoogleMaps;
using Mopups.Interfaces;
using CommunityToolkit.Maui.Core.Platform;
using Ridebase.Models;
using Ridebase.Services.Geocoding;
using Ridebase.ViewModels;
using Ridebase.Services;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace Ridebase.Pages.Rider;

public partial class MapHomePage 
{
    private IGeocodeGoogle geocodeGoogle;
    private Pin currentLocationPin;
    private Pin destinationPin = null;
    private Position position;
    private IKeyboardService keyboardService;
    private IPopupNavigation popupNavigation;
    private MapHomeViewModel mapHomeViewModel;
    private bool bottomSheetIsMini = true;

    public MapHomePage(MapHomeViewModel mapHomeViewModel,
        IGeocodeGoogle geocodeGoogle,
        IPopupNavigation navigation,
        IKeyboardService keyboardService)
	    {
		    InitializeComponent();

            homeMapControl.UiSettings.MyLocationButtonEnabled = true;
            this.geocodeGoogle = geocodeGoogle;
            this.mapHomeViewModel = mapHomeViewModel;
            popupNavigation = navigation;

            BindingContext = mapHomeViewModel;

        ShowBottomSheet(0.3);

        GetCurrentLocation();
	    }

    //Get current location name and write to console
    public async Task GetCurrentLocation()
    {
        try
        {
            FromLocationEntry.Placeholder = "Searching...";

            LocationWithAddress locationWithAddress
                = await geocodeGoogle.GetCurrentLocationWithAddressAsync();

            if (locationWithAddress != null)
            {
                position = new(locationWithAddress.Location.latitude, locationWithAddress.Location.longitude);
                homeMapControl.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(1000)));

                currentLocationPin = new Pin
                {
                    Label = "Current Location",
                    Position = position,
                    Type = PinType.Place,
                };

                FromLocationEntry.Placeholder = locationWithAddress.FormattedAddress;
            }
            else
            {
                FromLocationEntry.Placeholder = "Couldn't get your location";
            }
        }
        // Catch one of the following exceptions:
        //   FeatureNotSupportedException
        //   FeatureNotEnabledException
        //   PermissionException
        catch (Exception ex)
        {
            // Unable to get location
        }
        finally
        {
        }
    }

    //When the GoToLocationEntry is clicked, open the bottom sheet
    private void EntriesFocused(object sender, FocusEventArgs e)
    {
        ShowBottomSheet(0.8);
    }

    //Method when keyboard disappears, how the bottom sheet
    private void EntriesUnfocused(object sender, FocusEventArgs e)
    {
        ShowBottomSheet(0.3);
    }

    private void FromLocationEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        mapHomeViewModel.StartLocation = e.NewTextValue;
    }

    //Select a place from the collection view
    private void PlaceCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Place place)
        {
            //Clear all pins first if new pin will be selected
            homeMapControl.Pins.Clear();
            mapHomeViewModel.SelectPlace(place);

            //Add pin to the destination location
            homeMapControl.Pins.Add(new Pin
            {
                Label = place.displayName.text,
                Position = new Position(place.location.latitude, place.location.longitude),
                Type = PinType.Place
            });

            //Move the camera to show both pins
            MoveCamera(position, new Position(place.location.latitude, place.location.longitude));

            MainBottomSheet.Show(0.1);

            //Make selection null
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    //Method to move camera with parameters of current position and destination position
    private void MoveCamera(Position current, Position destination)
    {
        //Calculate distance between pin and current location in metres
        double distance = MauiLocation.CalculateDistance(new(current.Latitude, current.Longitude), new(destination.Latitude, destination.Longitude), DistanceUnits.Kilometers) * 1000;

        //Distance / 2 is the altitude from which to view the map
        //Adjust camera to show new pin and current location together. Calculate the appropriate distance that should be shown, with very smooth animation
        homeMapControl.MoveToRegion(MapSpan.FromCenterAndRadius(new Position((destination.Latitude + current.Latitude) / 2, (destination.Longitude + current.Longitude) / 2), Distance.FromMeters(distance / 2)), true);
    }

    private void ShowBottomSheet(double value)
    {
        MainBottomSheet.Show(value);
    }
}