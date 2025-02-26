using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Interfaces;
using Ridebase.Models;
using Ridebase.Pages;
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
    public IAuthenticationClient authenticationClient;

    public BaseViewModel()
    {
        CheckAuthenticationState();
    }
    
    //Login Command
    [RelayCommand]
    public async Task Login()
    {
        var loginResult = await authenticationClient.LoginAsync();

        //If login is successful
        if (loginResult.IsSuccess)
        {
            try
            {
                //Set the user details
                RidebaseUser = new User
                {
                    UserId = loginResult.Data.User.FindFirst("sub")?.Value,
                    UserName = loginResult.Data.User.FindFirst("name")?.Value,
                    Email = loginResult.Data.User.FindFirst("email")?.Value,
                    ImageUrl = loginResult.Data.User.FindFirst("picture")?.Value,
                    AccessToken = loginResult.Data.AccessToken
                };

                Console.WriteLine($"Print JWT: {loginResult.Data.AccessToken}");
                Console.WriteLine($"Print JWT Expiration: {loginResult.Data.AccessTokenExpiration}");

                //Save access token locally & rider id
                await SecureStorage.SetAsync("auth_token", loginResult.Data.AccessToken);
                await SecureStorage.SetAsync("userId", RidebaseUser.UserId);

                //Send access token to server
                var response = await authenticationClient.GetUserInfo(loginResult.Data.AccessToken);

                IsLoggedIn = true;
            }
            catch (Exception ex)
            {
                await AppShell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
        else
        {
            await AppShell.Current.DisplayAlert("Error", loginResult.ErrorMessage, "OK");
        }
    }

    private async void CheckAuthenticationState()
    {
        var token = await SecureStorage.GetAsync("auth_token");

        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            // Token is invalid or expired, clear storage
            ClearStorage();
        }
        else
        {
            IsLoggedIn = true;
        }
    }

    private bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Compare expiration time with current UTC time
        return jwtToken.ValidTo < DateTime.UtcNow;
    }

    [RelayCommand]
    public async Task LogoutUser()
    {
        await authenticationClient.LogoutAsync();

        ClearStorage();
    }

    public void ClearStorage()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("userId");
        SecureStorage.Remove("refresh_token");
    }
}
