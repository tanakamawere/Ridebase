using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    private readonly Auth0Client auth0Client;
    private readonly IAuthenticationClient authenticationClient;

    public AppShellViewModel(Auth0Client client, IAuthenticationClient _authClient)
    {
        auth0Client = client;
        authenticationClient = _authClient;
    }

    //Login Command
    [RelayCommand]
    private async Task Login()
    {
        var loginResult = await auth0Client.LoginAsync();

        //If login is successful
        if (!loginResult.IsError)
        {
            IsLoggedIn = true;

            //Set the user details
            RidebaseUser = new User
            {
                UserId = loginResult.User.FindFirst("sub")?.Value,
                UserName = loginResult.User.FindFirst("name")?.Value,
                Email = loginResult.User.FindFirst("email")?.Value,
                AccessToken = loginResult.AccessToken
            };

            //Save access token locally & rider id
            await SecureStorage.SetAsync("auth_token", loginResult.AccessToken);
            await SecureStorage.SetAsync("userId", RidebaseUser.UserId);            

            //Send access token to server
            var response = await authenticationClient.GetUserInfo(loginResult.AccessToken);

            if (response.IsSuccess)
            {
                //Get user information
                await AppShell.Current.DisplayAlert("Success", response.ToString(), "OK");
            }
            else
            {
                //Display error message
                await AppShell.Current.DisplayAlert("Error", response.ErrorMessage, "OK");
            }
        }
        else
        {
            await AppShell.Current.DisplayAlert("Error", loginResult.Error, "OK");
        }

        Console.WriteLine("Login Result: " + loginResult.AccessToken);

        if (loginResult.IsError)
            await AppShell.Current.DisplayAlert("Error", loginResult.Error, "OK");
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
