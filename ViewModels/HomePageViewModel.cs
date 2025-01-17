using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Interfaces;
using MPowerKit.GoogleMaps;
using Ridebase.Pages.Rider;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;

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

    public HomePageViewModel(IPlaces places, IGeocodeGoogle geocodeGoogle, IPopupNavigation navigation)
    {
        Title = "Map Page";
        placesApi = places;
        this.geocodeGoogle = geocodeGoogle;
        popupNavigation = navigation;

        GetCurrentLocation();
    }

    //Method to get user's current location
    public async Task GetCurrentLocation()
    {
        try
        {
            IsBusy = true;
            CurrentLocation = await geocodeGoogle.GetCurrentLocationWithAddressAsync();
            if (CurrentLocation != null)
            {
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

        //TODO: Send current location/place
        if (CurrentLocation is not null)
        {
            await Shell.Current.GoToAsync(nameof(SearchPage), true, new Dictionary<string, object>
            {
                {"currentLocation", CurrentLocation }
            });
        }
    }
}
