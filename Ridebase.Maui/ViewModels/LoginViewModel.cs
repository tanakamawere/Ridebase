using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Ridebase.Services;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly OidcLoginService _oidcLoginService;
    private readonly IUserSessionService _userSessionService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public LoginViewModel(
        OidcLoginService oidcLoginService,
        IUserSessionService userSessionService,
        ILogger<LoginViewModel> logger)
    {
        _oidcLoginService = oidcLoginService;
        _userSessionService = userSessionService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your username and password.";
            return;
        }

        IsBusy = true;
        try
        {
            var loginResult = await _oidcLoginService.LoginAsync(Username.Trim(), Password);

            if (loginResult.IsError)
            {
                ErrorMessage = loginResult.ErrorDescription ?? loginResult.Error ?? "Login failed.";
                _logger.LogWarning("Login failed: {Error}", loginResult.Error);
                return;
            }

            // Extract claims
            var userId = loginResult.User.FindFirst(c => c.Type == "sub")?.Value ?? Guid.NewGuid().ToString("N");
            var email = loginResult.User.FindFirst(c => c.Type == "email")?.Value
                     ?? loginResult.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                     ?? string.Empty;
            var displayName = loginResult.User.Identity?.Name ?? "Ridebase User";
            var pictureUrl = loginResult.User.FindFirst(c => c.Type == "picture")?.Value ?? string.Empty;

            await _userSessionService.SetAuthSessionAsync(
                userId,
                loginResult.AccessToken,
                loginResult.RefreshToken,
                displayName,
                email,
                pictureUrl);

            // Signal the AppShellViewModel with all data — avoids re-reading from storage
            WeakReferenceMessenger.Default.Send(new LoginSuccessMessage(new LoginSuccessData
            {
                UserId = userId,
                AccessToken = loginResult.AccessToken,
                DisplayName = displayName,
                Email = email,
                PictureUrl = pictureUrl
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception");
            ErrorMessage = "Unable to sign in right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToSignUpAsync()
    {
        await Shell.Current.GoToAsync("SignUpPage");
    }
}
