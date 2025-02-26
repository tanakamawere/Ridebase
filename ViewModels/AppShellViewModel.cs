using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    public AppShellViewModel(Auth0Client client, IAuthenticationClient _authClient)
    {
        authenticationClient = _authClient;
    }

    [RelayCommand]
    public void ChangeToDriverShellAsync()
    {
        Application.Current.OpenWindow(new Window(new DriverShell()));
    }
    [RelayCommand]
    public void GoToFleetPages()
    {

    }
}
