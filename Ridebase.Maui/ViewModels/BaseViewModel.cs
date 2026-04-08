using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Models.Ride;
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
    User? ridebaseUser;
    [ObservableProperty]
    private string authToken = string.Empty;
    [ObservableProperty]
    private string userId = string.Empty;
    //Services — set by derived-class constructors via DI; null! suppresses CS8618
    public IPopupNavigation popupNavigation = null!;
    public IRideApiClient rideApiClient = null!;
    public IStorageService storageService = null!;
    public IAuthService _authService = null!;
    protected ILogger Logger = null!;
    public IOnboardingApiClient onboardingApiClient = null!;
    public IUserSessionService userSessionService = null!;
    public IUserBootstrapService userBootstrapService = null!;
    public IRideStateStore rideStateStore = null!;
    public IRideRealtimeService rideRealtimeService = null!;

    public BaseViewModel()
    {
    }
}
