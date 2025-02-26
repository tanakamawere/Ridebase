using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Xpo.DB;
using GoogleApi;
using Mopups.Interfaces;
using MPowerKit.GoogleMaps;
using Ridebase.Helpers;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class HomePageViewModel : BaseViewModel
{
    [ObservableProperty]
    private LocationWithAddress currentLocation;
    [ObservableProperty]
    private CameraUpdate? _initialCameraPosition;

    [ObservableProperty]
    private Action<CameraUpdate> _moveCameraAction;

    [ObservableProperty]
    private Func<CameraUpdate, int, Task> _animateCameraFunc;
    private readonly GoogleMaps.Geocode.LocationGeocodeApi geolocationGoogle;

    public HomePageViewModel(IPopupNavigation navigation, GoogleMaps.Geocode.LocationGeocodeApi locationGeocodeApi, IStorageService _storage)
    {
        Title = "Map Page";
        geolocationGoogle = locationGeocodeApi;
        popupNavigation = navigation;
        storageService = _storage;

        GetCurrentLocation();
    }

    //Method to get user's current location
    public async Task GetCurrentLocation()
    {
        try
        {
            IsBusy = true;

            LocationService locationService = new LocationService();
            Location location = await locationService.GetCurrentLocationAsync();

            var response = await geolocationGoogle.QueryAsync(new GoogleApi.Entities.Maps.Geocoding.Location.Request.LocationGeocodeRequest
            {
                Location = new GoogleApi.Entities.Common.Coordinate(location.Latitude, location.Longitude),
                Key = Constants.googleMapsApiKey
            });

            if (response != null)
            {
                var firstResult = response.Results.FirstOrDefault();

                //Create current location object
                CurrentLocation = new LocationWithAddress
                {
                    Location = new Models.Location()
                    {
                        latitude = location.Latitude,
                        longitude = location.Longitude
                    },
                    FormattedAddress = firstResult.FormattedAddress
                };

                var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new(CurrentLocation.Location.latitude, CurrentLocation.Location.longitude), 15);

                await MoveCamera(cameraUpdate);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MoveCamera(CameraUpdate newPosition)
    {
        await AnimateCameraFunc(newPosition, 2000);
    }

    //Relay command to go to the search page
    [RelayCommand]
    public async Task GoToSearchPage()
    {
        if (IsBusy)
            return;

        //If user is not logged in, redirect to Auth0
        if (!await storageService.IsLoggedInAsync())
        {
            await App.Current.MainPage.DisplayAlert("Log in", "First login in the sidebar", "Ok");
            return;
        }

        if (CurrentLocation is not null)
        {
            await Shell.Current.GoToAsync(nameof(SearchPage), true, new Dictionary<string, object>
            {
                {"currentLocation", CurrentLocation }
            });
        }
    }
}
