using Auth0.OidcClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool isDriverMode;

    [ObservableProperty]
    private bool isRiderMode = true;

    public AppShellViewModel(
        Auth0Client authClient,
        ILogger<AppShellViewModel> logger,
        IOnboardingApiClient _onboardingApiClient,
        IUserSessionService _userSessionService,
        IUserBootstrapService _userBootstrapService)
    {
        authenticationClient = authClient;
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        userBootstrapService = _userBootstrapService;
        Logger = logger;
        CheckAuthenticationState();
    }

    [RelayCommand]
    public async Task SwitchToDriverModeAsync()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Driver);
        IsDriverMode = true;
        IsRiderMode = false;
        await Shell.Current.GoToAsync("//DriverHome");
    }

    [RelayCommand]
    public async Task SwitchToRiderModeAsync()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Rider);
        IsDriverMode = false;
        IsRiderMode = true;
        await Shell.Current.GoToAsync("//Home");
    }

    [RelayCommand]
    public Task GoToFleetPages()
    {
        return Shell.Current.GoToAsync("//DriverHome");
    }

    [RelayCommand]
    public async Task Login()
    {
        try
        {
            var loginResult = await authenticationClient.LoginAsync();
            if (loginResult.IsError)
            {
                await Application.Current.MainPage.DisplayAlert("Login Failed", loginResult.ErrorDescription, "OK");
                return;
            }

            var userId = loginResult.User.FindFirst(c => c.Type == "sub")?.Value ?? Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync("auth_token", loginResult.AccessToken);
            await SecureStorage.SetAsync("user_id", userId);

            if (!string.IsNullOrWhiteSpace(loginResult.RefreshToken))
            {
                await SecureStorage.SetAsync("refresh_token", loginResult.RefreshToken);
            }

            RidebaseUser = await userSessionService.BuildUserAsync(userId, loginResult.AccessToken, loginResult.User.Identity?.Name ?? "Ridebase User");
            IsLoggedIn = true;

            var bootstrap = await userBootstrapService.ResolveAfterLoginAsync(userId);
            await NavigateByBootstrapState(bootstrap);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Auth0 login failed, using local mock login fallback");
            var fallbackUserId = Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync("auth_token", $"mock_{fallbackUserId}");
            await SecureStorage.SetAsync("user_id", fallbackUserId);
            IsLoggedIn = true;

            var bootstrap = await userBootstrapService.ResolveAfterLoginAsync(fallbackUserId);
            await NavigateByBootstrapState(bootstrap);
        }
    }

    private async void CheckAuthenticationState()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");

            if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
            {
                ClearStorage();
                return;
            }

            IsLoggedIn = true;
            var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
            var bootstrap = await userBootstrapService.ResolveAfterLoginAsync(userId);
            await NavigateByBootstrapState(bootstrap);
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "CheckAuthenticationState failed; treating as logged out");
            ClearStorage();
        }
    }

    private bool IsTokenExpired(string token)
    {
        if (token.StartsWith("mock_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.ValidTo < DateTime.UtcNow;
    }

    [RelayCommand]
    public async Task LogoutUser()
    {
        try
        {
            await authenticationClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "Auth0 logout failed in mock mode");
        }

        ClearStorage();
        IsLoggedIn = false;
        IsDriverMode = false;
        IsRiderMode = true;
        await Shell.Current.GoToAsync("//Home");
    }

    public void ClearStorage()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("user_id");
        SecureStorage.Remove("refresh_token");
    }

    private async Task NavigateByBootstrapState(UserBootstrapState bootstrap)
    {
        if (!bootstrap.IsOnboarded)
        {
            await Shell.Current.GoToAsync(nameof(OnboardingProfilePage));
            return;
        }

        IsDriverMode = bootstrap.Role == AppUserRole.Driver;
        IsRiderMode = bootstrap.Role == AppUserRole.Rider;

        if (bootstrap.Role == AppUserRole.Driver)
        {
            await Shell.Current.GoToAsync("//DriverHome");
        }
        else
        {
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
