using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Services.RideService;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    private readonly Auth0Client auth0Client;

    public AppShellViewModel(Auth0Client client, IRideService rideService)
    {
        auth0Client = client;
        this.rideService = rideService;
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
            var response = await rideService.PostAccessToken(loginResult.AccessToken);

            //Get user information
            await AppShell.Current.DisplayAlert("Success", response.ToString(), "OK");
        }
        else
        {
            await AppShell.Current.DisplayAlert("Error", loginResult.Error, "OK");
        }

        Console.WriteLine("Login Result: " + loginResult.AccessToken);

        if (loginResult.IsError)
            await AppShell.Current.DisplayAlert("Error", loginResult.Error, "OK");
    }
}
