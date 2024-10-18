using Maui.GoogleMaps;
using Mopups.Interfaces;
using Ridebase.Services.Geocoding;
using Ridebase.ViewModels;
using BaseResponse = Ridebase.Services.Geocoding.BaseResponse;

namespace Ridebase.Pages.Rider;

public partial class MapHomePage 
{
    private IGeocodeGoogle geocodeGoogle;
    private Pin currentLocationPin;
    private Pin destinationPin = null;
    private Position position;
    private IPopupNavigation popupNavigation;
    private MapHomeViewModel mapHomeViewModel;

    public MapHomePage(MapHomeViewModel mapHomeViewModel,
        IGeocodeGoogle geocodeGoogle,
        IPopupNavigation navigation)
	    {
		    InitializeComponent();

            homeMapControl.UiSettings.MyLocationButtonEnabled = true;
            this.geocodeGoogle = geocodeGoogle;
            this.mapHomeViewModel = mapHomeViewModel;
            popupNavigation = navigation;

            BindingContext = mapHomeViewModel;

            GetCurrentLocation();
	    }

    //Get current location name and write to console
    public async Task GetCurrentLocation()
    {
        try
        {
            FromLocationEntry.Text = "Searching...";

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

                homeMapControl.Pins.Add(currentLocationPin);

                FromLocationEntry.Text = locationWithAddress.FormattedAddress;
            }
            else
            {
                FromLocationEntry.Text = "Couldn't get your location";
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

    private void FromLocationEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        mapHomeViewModel.StartLocation = e.NewTextValue;
    }
}