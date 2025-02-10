using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    private readonly IAuthenticationClient authenticationClient;

    public AppShellViewModel(Auth0Client client, IAuthenticationClient _authClient)
    {
        authenticationClient = _authClient;
    }

    //Login Command
    [RelayCommand]
    private async Task Login()
    {
        var loginResult = await authenticationClient.LoginAsync();

        //If login is successful
        if (!loginResult.IsSuccess)
        {
            IsLoggedIn = true;

            //Set the user details
            RidebaseUser = new User
            {
                UserId = loginResult.Data.User.FindFirst("sub")?.Value,
                UserName = loginResult.Data.User.FindFirst("name")?.Value,
                Email = loginResult.Data.User.FindFirst("email")?.Value,
                AccessToken = loginResult.Data.AccessToken
            };

            //Save access token locally & rider id
            await SecureStorage.SetAsync("auth_token", loginResult.Data.AccessToken);
            await SecureStorage.SetAsync("userId", RidebaseUser.UserId);            

            //Send access token to server
            var response = await authenticationClient.GetUserInfo(loginResult.Data.AccessToken);

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
            await AppShell.Current.DisplayAlert("Error", loginResult.ErrorMessage, "OK");
        }

        Console.WriteLine("Login Result: " + loginResult.Data.AccessToken);

        if (!loginResult.IsSuccess)
            await AppShell.Current.DisplayAlert("Error", loginResult.ErrorMessage, "OK");
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
