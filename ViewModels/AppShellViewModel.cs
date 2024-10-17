﻿using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel: BaseViewModel
{
    private readonly Auth0Client auth0Client;

    public AppShellViewModel(Auth0Client client)
    {
        auth0Client = client;
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

            await SecureStorage.SetAsync("auth_token", loginResult.AccessToken);

            await App.Current.MainPage.DisplayAlert("Success", "You are now logged in", "OK");
        }
        else
        {
            await App.Current.MainPage.DisplayAlert("Error", loginResult.Error, "OK");
        }

        Console.WriteLine("Login Result: " + loginResult.AccessToken);

        if (loginResult.IsError)
            await App.Current.MainPage.DisplayAlert("Error", loginResult.Error, "OK");
    }
}