using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Duende.IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Pages.Onboarding;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Ridebase.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool isDriverMode;

    [ObservableProperty]
    private bool isRiderMode = true;

    [ObservableProperty]
    private bool hasEmail;

    [ObservableProperty]
    private string currentModeLabel = "Rider";

    public AppShellViewModel(
        OidcClient authClient,
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
        try
        {
            var state = await userSessionService.GetStateAsync();
            if (!state.IsOnboarded)
            {
                await Shell.Current.GoToAsync(nameof(OnboardingDriverPage));
                Shell.Current.FlyoutIsPresented = false;
                return;
            }

            await userSessionService.SetRoleAsync(AppUserRole.Driver);
            IsDriverMode = true;
            IsRiderMode = false;
            CurrentModeLabel = "Driver";
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//DriverHome");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to switch to driver mode");
            await ShowAlertAsync("Error", "Could not switch to driver mode. Please try again.");
        }
    }

    [RelayCommand]
    public async Task SwitchToRiderModeAsync()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Rider);
        IsDriverMode = false;
        IsRiderMode = true;
        CurrentModeLabel = "Rider";
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("//Home");
    }

    [RelayCommand]
    public async Task GoToHome()
    {
        Shell.Current.FlyoutIsPresented = false;
        if (IsDriverMode)
            await Shell.Current.GoToAsync("//DriverHome");
        else
            await Shell.Current.GoToAsync("//Home");
    }

    [RelayCommand]
    public async Task GoToRideHistory()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync(nameof(RideHistoryPage));
    }

    [RelayCommand]
    public async Task GoToWallet()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync(nameof(WalletPage));
    }

    [RelayCommand]
    public async Task GoToProfile()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    [RelayCommand]
    public async Task GoToSupport()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync(nameof(SupportPage));
    }

    [RelayCommand]
    public Task GoToFleetPages()
    {
        return Shell.Current.GoToAsync("//DriverHome");
    }

    [RelayCommand]
    public async Task Login()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var loginResult = await authenticationClient.LoginAsync();
            if (loginResult.IsError)
            {
                await ShowAlertAsync("Login Failed", loginResult.ErrorDescription);
                return;
            }

            var userId = loginResult.User.FindFirst(c => c.Type == "sub")?.Value ?? Guid.NewGuid().ToString("N");
            var email = loginResult.User.FindFirst(c => c.Type == "email")?.Value
                     ?? loginResult.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                     ?? string.Empty;
            var displayName = loginResult.User.Identity?.Name ?? "Ridebase User";
            var pictureUrl = loginResult.User.FindFirst(c => c.Type == "picture")?.Value ?? string.Empty;

            await userSessionService.SetAuthSessionAsync(userId, loginResult.AccessToken, loginResult.RefreshToken, displayName, email, pictureUrl);

            RidebaseUser = await userSessionService.GetCachedUserAsync(loginResult.AccessToken)
                ?? await userSessionService.BuildUserAsync(userId, loginResult.AccessToken, displayName);
            HasEmail = !string.IsNullOrWhiteSpace(RidebaseUser.Email);
            IsLoggedIn = true;

            Shell.Current.FlyoutIsPresented = false;

            var bootstrap = await userBootstrapService.ResolveAfterLoginAsync(userId);
            ApplyBootstrapDisplayName(bootstrap);
            await NavigateByBootstrapState(bootstrap);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Auth0 login failed");
            await ShowAlertAsync("Login Failed", "Unable to complete authentication right now. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void CheckAuthenticationState()
    {
        IsBusy = true;
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");

            if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
            {
                await ResetToLoggedOutStateAsync();
                return;
            }

            var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
            RidebaseUser = await userSessionService.GetCachedUserAsync(token)
                ?? await userSessionService.BuildUserAsync(userId, token, "Ridebase User");
            HasEmail = !string.IsNullOrWhiteSpace(RidebaseUser?.Email);
            IsLoggedIn = true;
            var bootstrap = await userBootstrapService.ResolveAfterLoginAsync(userId);
            ApplyBootstrapDisplayName(bootstrap);
            await NavigateByBootstrapState(bootstrap);
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "CheckAuthenticationState failed; treating as logged out");
            await ResetToLoggedOutStateAsync();
        }
        finally
        {
            IsBusy = false;
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

        await ResetToLoggedOutStateAsync(navigateHome: true);
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
        CurrentModeLabel = bootstrap.Role == AppUserRole.Driver ? "Driver" : "Rider";

        if (bootstrap.Role == AppUserRole.Driver)
        {
            await Shell.Current.GoToAsync("//DriverHome");
        }
        else
        {
            await Shell.Current.GoToAsync("//Home");
        }
    }

    private static Task ShowAlertAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, "OK");
    }

    private void ApplyBootstrapDisplayName(UserBootstrapState bootstrap)
    {
        if (RidebaseUser is null || string.IsNullOrWhiteSpace(bootstrap.FullName))
        {
            return;
        }

        RidebaseUser.UserName = bootstrap.FullName;
    }

    private async Task ResetToLoggedOutStateAsync(bool navigateHome = false)
    {
        await userSessionService.ClearSessionAsync();

        IsLoggedIn = false;
        HasEmail = false;
        IsDriverMode = false;
        IsRiderMode = true;
        CurrentModeLabel = "Rider";
        RidebaseUser = null;

        if (navigateHome)
        {
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
