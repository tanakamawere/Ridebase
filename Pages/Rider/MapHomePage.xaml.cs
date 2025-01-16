using Maui.GoogleMaps;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Services.Geocoding;
using System.Reflection;
using Ridebase.ViewModels;
using Ridebase.Services;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;
using CommunityToolkit.Maui.Core.Platform;
using Ridebase.Services.Directions;

namespace Ridebase.Pages.Rider;

public partial class MapHomePage 
{
    private IGeocodeGoogle geocodeGoogle;
    private Pin currentLocationPin;
    private readonly Pin destinationPin = null;
    private Position currentPosition;
    private Position startPosition;
    private readonly IKeyboardService keyboardService;
    private readonly IPopupNavigation popupNavigation;
    private readonly IDirections directionsApi;
    private readonly MapHomeViewModel mapHomeViewModel;
    private Models.Place startPlace;

    public MapHomePage(MapHomeViewModel mapHomeViewModel,
        IGeocodeGoogle geocodeGoogle,
        IDirections directionsApi,
        IPopupNavigation navigation,
        IKeyboardService keyboardService)
	    {
		    InitializeComponent();

            homeMapControl.UiSettings.MyLocationButtonEnabled = true;
            this.geocodeGoogle = geocodeGoogle;
            this.mapHomeViewModel = mapHomeViewModel;
        this.directionsApi = directionsApi;
        popupNavigation = navigation;

            BindingContext = mapHomeViewModel;

            ShowBottomSheet(0.3);

            SetTheme();
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

            //Locations with address to Place object and send that to the view model
            startPlace = new()
            {
                displayName = new()
                {
                    text = locationWithAddress.FormattedAddress,
                    languageCode = "en"
                },
                location = new()
                {
                    latitude = locationWithAddress.Location.latitude,
                    longitude = locationWithAddress.Location.longitude
                },
                formattedAddress = locationWithAddress.FormattedAddress,
                id = "Current Location"
            };

            //Set place to the view model
            mapHomeViewModel.StartPlace = startPlace;

            if (locationWithAddress != null)
            {
                currentPosition = new(locationWithAddress.Location.latitude, locationWithAddress.Location.longitude);
                homeMapControl.MoveToRegion(MapSpan.FromCenterAndRadius(currentPosition, Maui.GoogleMaps.Distance.FromMeters(1000)));

                currentLocationPin = new Pin
                {
                    Label = "Current Location",
                    Position = currentPosition,
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
        if (e.CurrentSelection.FirstOrDefault() is Models.Place place)
        {
            if (FromLocationEntry.Text == "")
            {
                //Means user wants to be taken from where they are
                startPosition = currentPosition;
            }

            //User wants to choose starting from location
            if (FromLocationEntry.IsFocused)
            {
                startPosition = new Position(place.location.latitude, place.location.longitude);

                //Create pin for that location
                homeMapControl.Pins.Add(new Pin
                {
                    Label = place.displayName.text,
                    Position = new Position(place.location.latitude, place.location.longitude),
                    Type = PinType.Place
                });

                //Set entry text to the name of the place
                FromLocationEntry.Text = place.displayName.text;

                //Start location update in the viewmodel
                mapHomeViewModel.StartPlace = place;

                //Make selection null
                ((CollectionView)sender).SelectedItem = null;
            }
            else
            {
                //Check which entry was focused between the go to and destination entries
                FromLocationEntry.HideKeyboardAsync(CancellationToken.None);
                GoToLocationEntry.HideKeyboardAsync(CancellationToken.None);

                //Means user wants to be taken from where they are
                startPosition = currentPosition;

                //if number of pins inside the map pins is greater than 2, it means they have changed start location
                if (homeMapControl.Pins.Count > 1)
                {
                    //remove first pin in the list
                    homeMapControl.Pins.RemoveAt(0);
                }

                mapHomeViewModel.SelectDestinationPlace(place);

                //Clear the searchQuery in the view model
                mapHomeViewModel.SearchQuery = string.Empty;
                //Now set the entry text to the display name of the selected place
                GoToLocationEntry.Text = place.displayName.text;

                //Add pin to the destination location
                homeMapControl.Pins.Add(new Pin
                {
                    Label = place.displayName.text,
                    Position = new Position(place.location.latitude, place.location.longitude),
                    Type = PinType.Place
                });

                //Move the camera to show both pins
                MoveCamera(startPosition, new Position(place.location.latitude, place.location.longitude));
                SetRideConfirmationState(place.id);

                //Make selection null
                ((CollectionView)sender).SelectedItem = null;
            }
        }
    }

    //Method to move camera with parameters of current position and destination position
    private void MoveCamera(Position current, Position destination)
    {
        //Calculate distance between pin and current location in metres
        double distance = MauiLocation.CalculateDistance(new(current.Latitude, current.Longitude), new(destination.Latitude, destination.Longitude), DistanceUnits.Kilometers) * 1000;

        //Distance / 2 is the altitude from which to view the map
        //Adjust camera to show new pin and current location together. Calculate the appropriate distance that should be shown, with very smooth animation
        homeMapControl.MoveToRegion
            (MapSpan.FromCenterAndRadius
                (new Position
                    ((destination.Latitude + current.Latitude) / 2, (destination.Longitude + current.Longitude) / 2), Maui.GoogleMaps.Distance.FromMeters(distance / 1.75)
                ), true);
    }

    //Method to make changes to the display for the ride confirmation
    private async Task SetRideConfirmationState(string selectedPlaceId)
    {
        //Resize bottom sheet
        ShowBottomSheet(0.2);

        //Display the ride confirmation side
        mapHomeViewModel.IsLocationSheetVisible = false;

        //Hide the Shell nav bar on the page
        Shell.SetNavBarIsVisible(this, false);

        //Add popup for destination editing

        //Draw polyline path
        var response = await directionsApi.GetDirections($"{startPosition.Latitude},{startPosition.Longitude}", $"place_id:{selectedPlaceId}");

        if (response != null)
        {
            Maui.GoogleMaps.Polyline polyline = new()
            {
                StrokeColor = Color.Parse("#9c1ee9"),
                StrokeWidth = 5f,
            };

            foreach (var step in response.Data.routes.First().legs.First().steps)
            {
                polyline.Positions.Add(new Position(step.start_location.lat, step.start_location.lng));
            }

            homeMapControl.Polylines.Add(polyline);
        }
    }

    private void ShowBottomSheet(double value)
    {
        locationSelectionSheet.Show(value);
    }
    private void SetTheme()
    {
        //Determine if it is dark mode
        if (Application.Current.RequestedTheme == AppTheme.Dark)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream($"Ridebase.darkmap.json");
            string styleFile;
            using (var reader = new StreamReader(stream))
            {
                styleFile = reader.ReadToEnd();
            }
            //Set the theme of the map
            homeMapControl.MapStyle = MapStyle.FromJson(styleFile);
        }
        else
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream($"Ridebase.lightmap.json");
            string styleFile;
            using (var reader = new StreamReader(stream))
            {
                styleFile = reader.ReadToEnd();
            }
            //Set the theme of the map
            homeMapControl.MapStyle = MapStyle.FromJson(styleFile);
        }
    }

    private void LocationEditBorderTapped(object sender, TappedEventArgs e)
    {
        //Remove previous polyline

        //If a user taps on the border, it means they want to edit the start and end locations
        mapHomeViewModel.IsLocationSheetVisible = true;
        Shell.SetNavBarIsVisible(this, true);
        ShowBottomSheet(0.8);
    }
}
