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
    public IAuthenticationClient authenticationClient;
    public IOnboardingApiClient onboardingApiClient;

    public BaseViewModel()
    {
        CheckAuthenticationState();
    }
    
    //Login Command
    [RelayCommand]
    public async Task Login()
    {
        Logger?.LogInformation("Starting login process");
        // Use Auth0 
        try
        {
            var loginResult = await authenticationClient.LoginAsync();
            if (loginResult.IsError)
            {
                Logger?.LogWarning("Login failed: {ErrorDescription}", loginResult.ErrorDescription);
                // Handle login error (e.g., show an error message) using the inbuilt shell
                await Application.Current.MainPage.DisplayAlert("Login Failed", loginResult.ErrorDescription, "OK");
            }
            else
            {
                Logger?.LogInformation("Login successful for user");
                // Store the token and user information securely
                await SecureStorage.SetAsync("auth_token", loginResult.AccessToken);
                await SecureStorage.SetAsync("userId", loginResult.User.FindFirst(c => c.Type == "sub")?.Value ?? string.Empty);
                if (loginResult.RefreshToken != null)
                {
                    await SecureStorage.SetAsync("refresh_token", loginResult.RefreshToken);
                }

                IsLoggedIn = true;

                // Check if the user has completed onboarding
                if (onboardingApiClient != null)
                {
                    var onboardingStatus = await onboardingApiClient.CheckOnboardingStatusAsync(RidebaseUser.UserId);
                    if (!onboardingStatus.IsSuccess || !onboardingStatus.Data)
                    {
                        await Shell.Current.GoToAsync(nameof(OnboardingProfilePage));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during login process");
            // Handle exceptions (e.g., network issues) using the inbuilt shell
            await Application.Current.MainPage.DisplayAlert("Login Error", ex.Message, "OK");
        }
    }

    private async void CheckAuthenticationState()
    {
        Logger?.LogInformation("Checking authentication state");
        var token = await SecureStorage.GetAsync("auth_token");

        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            Logger?.LogInformation("Token is invalid or expired, clearing storage");
            // Token is invalid or expired, clear storage
            ClearStorage();
        }
        else
        {
            Logger?.LogInformation("User is authenticated");
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
        Logger?.LogInformation("Starting logout process");
        try
        {
            await authenticationClient.LogoutAsync();
            Logger?.LogInformation("Logout successful");
            ClearStorage();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during logout");
            throw;
        }
    }

    public void ClearStorage()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("userId");
        SecureStorage.Remove("refresh_token");
    }
}
