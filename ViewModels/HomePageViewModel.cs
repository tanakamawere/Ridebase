using Mopups.Interfaces;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;

namespace Ridebase.ViewModels;

public class HomePageViewModel : BaseViewModel
{
    public HomePageViewModel(IPlaces places, IGeocodeGoogle geocodeGoogle, IPopupNavigation navigation)
    {
        Title = "Map Page";
        placesApi = places;
        this.geocodeGoogle = geocodeGoogle;
        popupNavigation = navigation;
    }
}
