using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Ridebase.Services;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

public partial class SignUpViewModel : ObservableObject
{
    private readonly AuthentikEnrollmentService _enrollmentService;
    private readonly OidcLoginService _oidcLoginService;
    private readonly IUserSessionService _userSessionService;
    private readonly ILogger<SignUpViewModel> _logger;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public SignUpViewModel(
        AuthentikEnrollmentService enrollmentService,
        OidcLoginService oidcLoginService,
        IUserSessionService userSessionService,
        ILogger<SignUpViewModel> logger)
    {
        _enrollmentService = enrollmentService;
        _oidcLoginService = oidcLoginService;
        _userSessionService = userSessionService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task SignUpAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        try
        {
            // Step 1 — Create account via Authentik
            var result = await _enrollmentService.SignUpAsync(Email.Trim(), Username.Trim(), Password);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Sign-up failed.";
                return;
            }

            // Step 2 — Navigate immediately so the user starts filling profile
            await Shell.Current.GoToAsync("OnboardingProfilePage");

            // Step 3 — Auto-login in the background while user fills in their details
            _ = BackgroundLoginAsync(Username.Trim(), Password, Email.Trim());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIGNUP] EXCEPTION: {ex}");
            _logger.LogError(ex, "Sign-up exception");
            ErrorMessage = "Unable to create account right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BackgroundLoginAsync(string username, string password, string email)
    {
        try
        {
            var loginResult = await _oidcLoginService.LoginAsync(username, password);

            if (loginResult.IsError)
            {
                _logger.LogWarning("Background auto-login failed: {Error}", loginResult.Error);
                return;
            }

            Console.WriteLine($"[SIGNUP] Background login complete, persisting session");
            var userId = loginResult.User?.FindFirst(c => c.Type == "sub")?.Value ?? Guid.NewGuid().ToString("N");
            var userEmail = loginResult.User?.FindFirst(c => c.Type == "email")?.Value
                         ?? loginResult.User?.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                         ?? email;
            var displayName = loginResult.User?.Identity?.Name ?? username;
            var pictureUrl = loginResult.User?.FindFirst(c => c.Type == "picture")?.Value ?? string.Empty;

            await _userSessionService.SetAuthSessionAsync(
                userId, loginResult.AccessToken, loginResult.RefreshToken,
                displayName, userEmail, pictureUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background auto-login exception");
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
