using CommunityToolkit.Mvvm.ComponentModel;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Services.Interfaces;

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
    [ObservableProperty]
    private string authToken;
    [ObservableProperty]
    private string userId;
    //Services
    public IPopupNavigation popupNavigation;
    public IRideApiClient rideService;

    public BaseViewModel()
    {
        AuthToken = SecureStorage.GetAsync("auth_token").Result;
        UserId = SecureStorage.GetAsync("userId").Result;

        //Check if access token is available in secure storage, if so, user is logged in
        if (!string.IsNullOrEmpty(authToken))
        {
            IsLoggedIn = true;
        }
    }
}
