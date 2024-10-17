using Maui.GoogleMaps;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Services;
using Ridebase.ViewModels;
using BaseResponse = Ridebase.Models.BaseResponse;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace Ridebase.Pages.Rider;

public partial class MapHomePage 
{
    private IGeocodeGoogle geocodeGoogle;
    private Location location;
    private Pin currentLocationPin;
    private Pin destinationPin = null;
    private Position position;
    private IPopupNavigation popupNavigation;

    public MapHomePage(MapHomeViewModel mapHomeViewModel,
        IGeocodeGoogle geocodeGoogle,
        IPopupNavigation navigation)
	    {
		    InitializeComponent();

            homeMapControl.UiSettings.MyLocationButtonEnabled = true;
            this.geocodeGoogle = geocodeGoogle;

            popupNavigation = navigation;
        BindingContext = mapHomeViewModel;

            GetCurrentLocation();
	    }

    //Get current location name and write to console
    public async Task GetCurrentLocation()
    {
        try
        {
            currentLocationLabel.Text = "Searching for your current location";

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

            location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                string locationAddress = $"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}";
                position = new(location.Latitude, location.Longitude);
                homeMapControl.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(1000)));

                currentLocationPin = new Pin
                {
                    Label = "Current Location",
                    Position = position,
                    Type = PinType.Place,
                };

                homeMapControl.Pins.Add(currentLocationPin);

                BaseResponse locations = await geocodeGoogle.GetPlacemarksAsync(location.Latitude, location.Longitude);
                var baseResponse = locations.results.FirstOrDefault();
                currentLocationLabel.Text = $"Your current location: {baseResponse.formatted_address}";
            }
            else
            {
                currentLocationLabel.Text = "Couldn't get your location";
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
}