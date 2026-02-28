using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

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
    public IRideApiClient rideApiClient;
    public IStorageService storageService;
    public Auth0Client authenticationClient;
    protected ILogger Logger;
    public IOnboardingApiClient onboardingApiClient;

    public BaseViewModel()
    {
    }
}
