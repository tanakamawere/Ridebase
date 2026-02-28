using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    public AppShellViewModel(Auth0Client client, IAuthenticationClient _authClient, IOnboardingApiClient _onboardingApiClient)
    public AppShellViewModel(Auth0Client authenticationClient, ILogger<AppShellViewModel> logger)
    {
        authenticationClient = _authClient;
        onboardingApiClient = _onboardingApiClient;
        Logger = logger;
        this.authenticationClient = authenticationClient;
    }

    [RelayCommand]
    public void ChangeToDriverShellAsync()
    {
        Logger.LogInformation("Switching to Driver Shell");
        try
        {
            Application.Current.OpenWindow(new Window(new DriverShell()));
            Logger.LogInformation("Successfully switched to Driver Shell");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error switching to Driver Shell");
        }
    }
    [RelayCommand]
    public void GoToFleetPages()
    {

    }
}
