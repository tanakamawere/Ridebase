using CommunityToolkit.Mvvm.ComponentModel;
using IdentityModel.OidcClient;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;
using Ridebase.Services.RideService;
using System.Security.Principal;

namespace Ridebase.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    bool isBusy;
    [ObservableProperty]
    bool isLoggedIn = false;
    [ObservableProperty]
    string title = string.Empty;
    [ObservableProperty]
    User ridebaseUser;

    //Services
    public IPopupNavigation popupNavigation;
    public IGeocodeGoogle geocodeGoogle;
    public IPlaces placesApi;
    public IRideService rideService;

    public BaseViewModel()
    {
        //Check if access token is available in secure storage, if so, user is logged in
        if (!string.IsNullOrEmpty(SecureStorage.GetAsync("auth_token").Result))
        {
            IsLoggedIn = true;
        }
    }
}
