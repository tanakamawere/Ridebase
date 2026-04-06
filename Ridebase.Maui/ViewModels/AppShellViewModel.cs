using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages;
using Ridebase.Pages.Auth;
using Ridebase.Pages.Onboarding;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.Interfaces;
using System.Diagnostics;
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

    private readonly OidcLoginService oidcLoginService;

    public AppShellViewModel(
        ILogger<AppShellViewModel> logger,
        IOnboardingApiClient _onboardingApiClient,
        IUserSessionService _userSessionService,
        IUserBootstrapService _userBootstrapService,
        OidcLoginService _oidcLoginService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        userBootstrapService = _userBootstrapService;
        oidcLoginService = _oidcLoginService;
        Logger = logger;

        // Listen for successful login from LoginViewModel
        WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, async (_, msg) =>
        {
            await CompleteLoginAsync(msg.Value);
        });

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
        await Shell.Current.GoToAsync(nameof(LoginPage));
        Shell.Current.FlyoutIsPresented = false;
    }

    private async Task CompleteLoginAsync(LoginSuccessData data)
    {
        IsBusy = true;
        try
        {
            // Build user directly from message data — no SecureStorage round-trips
            RidebaseUser = new Models.User
            {
                UserId = data.UserId,
                UserName = data.DisplayName,
                AccessToken = data.AccessToken,
                Email = data.Email,
                ImageUrl = data.PictureUrl
            };
            HasEmail = !string.IsNullOrWhiteSpace(data.Email);
            IsLoggedIn = true;

            // Start bootstrap in parallel with navigating to Home
            var bootstrapTask = userBootstrapService.ResolveAfterLoginAsync(data.UserId);
            await Shell.Current.GoToAsync("//Home");

            try
            {
                var bootstrap = await bootstrapTask;
                ApplyBootstrapDisplayName(bootstrap);
                await NavigateByBootstrapState(bootstrap);
            }
            catch (Exception bootstrapEx)
            {
                // Bootstrap failure should not block the user — they're already on Home
                Logger?.LogWarning(bootstrapEx, "Post-login bootstrap failed: {Message}", bootstrapEx.Message);
                Debug.WriteLine($"[Bootstrap] {bootstrapEx}");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Post-login flow failed");
            await ShowAlertAsync("Error", "Login succeeded but setup failed. Please try again.");
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

            if (string.IsNullOrEmpty(token))
            {
                await ResetToLoggedOutStateAsync();
                return;
            }

            // If the access token is expired, try a silent refresh first
            if (IsTokenExpired(token))
            {
                token = await TrySilentRefreshAsync();
                if (token is null)
                {
                    await ResetToLoggedOutStateAsync();
                    return;
                }
            }

            Console.WriteLine($"[RELOGIN] Access token: {token}");

            var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
            RidebaseUser = await userSessionService.GetCachedUserAsync(token)
                ?? await userSessionService.BuildUserAsync(userId, token, "Ridebase User");
            HasEmail = !string.IsNullOrWhiteSpace(RidebaseUser?.Email);
            IsLoggedIn = true;

            // Navigate immediately using cached state for a fast startup
            var cachedState = await userSessionService.GetStateAsync();
            ApplyBootstrapDisplayName(cachedState);
            await NavigateByBootstrapState(cachedState);

            // Refresh state from server in background (non-blocking)
            _ = RefreshBootstrapInBackgroundAsync(userId, cachedState);
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

    /// <summary>
    /// Attempts a silent token refresh using the stored refresh token.
    /// Returns the new access token on success, or null if refresh fails.
    /// </summary>
    private async Task<string?> TrySilentRefreshAsync()
    {
        var refreshToken = await SecureStorage.GetAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        var result = await oidcLoginService.RefreshAsync(refreshToken);
        if (result.IsError)
        {
            Logger?.LogWarning("Silent refresh failed: {Error}", result.Error);
            return null;
        }

        // Persist the new tokens
        var tasks = new List<Task>
        {
            SecureStorage.SetAsync("auth_token", result.AccessToken)
        };
        if (!string.IsNullOrEmpty(result.RefreshToken))
            tasks.Add(SecureStorage.SetAsync("refresh_token", result.RefreshToken));
        await Task.WhenAll(tasks);

        return result.AccessToken;
    }

    private async Task RefreshBootstrapInBackgroundAsync(string userId, UserBootstrapState cachedState)
    {
        try
        {
            var freshState = await userBootstrapService.ResolveAfterLoginAsync(userId);
            MainThread.BeginInvokeOnMainThread(() => ApplyBootstrapDisplayName(freshState));

            // Only re-navigate if role or onboarding status changed
            if (freshState.Role != cachedState.Role || freshState.IsOnboarded != cachedState.IsOnboarded)
            {
                await MainThread.InvokeOnMainThreadAsync(() => NavigateByBootstrapState(freshState));
            }
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "Background bootstrap refresh failed");
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
