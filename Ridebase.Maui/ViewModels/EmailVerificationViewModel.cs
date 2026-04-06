using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages.Onboarding;
using Ridebase.Services;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels;

[QueryProperty(nameof(UserEmail), "UserEmail")]
[QueryProperty(nameof(UserId), "UserId")]
[QueryProperty(nameof(AccessToken), "AccessToken")]
[QueryProperty(nameof(DisplayName), "DisplayName")]
[QueryProperty(nameof(PictureUrl), "PictureUrl")]
[QueryProperty(nameof(SelectedRole), "SelectedRole")]
public partial class EmailVerificationViewModel : ObservableObject
{
    private readonly IOnboardingApiClient _onboardingApiClient;
    private readonly IUserSessionService _userSessionService;
    private readonly OidcLoginService _oidcLoginService;
    private readonly ILogger<EmailVerificationViewModel> _logger;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isResending;

    [ObservableProperty]
    private string statusMessage = "We've sent a verification code to your email. Enter the 6-digit code below.";

    [ObservableProperty]
    private string busyMessage = string.Empty;

    [ObservableProperty]
    private bool isVerified;

    [ObservableProperty]
    private string otpCode = string.Empty;

    public string? UserEmail { get; set; }
    public string? UserId { get; set; }
    public string? AccessToken { get; set; }
    public string? DisplayName { get; set; }
    public string? PictureUrl { get; set; }
    public string? SelectedRole { get; set; }

    public EmailVerificationViewModel(
        IOnboardingApiClient onboardingApiClient,
        IUserSessionService userSessionService,
        OidcLoginService oidcLoginService,
        ILogger<EmailVerificationViewModel> logger)
    {
        _onboardingApiClient = onboardingApiClient;
        _userSessionService = userSessionService;
        _oidcLoginService = oidcLoginService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task VerifyOtpAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Length < 6)
        {
            StatusMessage = "Please enter the full 6-digit code.";
            return;
        }

        IsBusy = true;
        BusyMessage = "Verifying code...";
        try
        {
            var result = await _onboardingApiClient.VerifyEmailOtpAsync(OtpCode.Trim());

            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Invalid or expired code. Please try again.";
                return;
            }

            IsVerified = true;
            StatusMessage = "Email verified! Taking you in...";

            // Refresh token so we get a fresh one (original may be near expiry)
            var refreshToken = await SecureStorage.GetAsync("refresh_token");
            var freshAccessToken = AccessToken ?? string.Empty;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshResult = await _oidcLoginService.RefreshAsync(refreshToken);
                if (!refreshResult.IsError)
                {
                    freshAccessToken = refreshResult.AccessToken;
                    await SecureStorage.SetAsync("auth_token", refreshResult.AccessToken);
                    if (!string.IsNullOrEmpty(refreshResult.RefreshToken))
                        await SecureStorage.SetAsync("refresh_token", refreshResult.RefreshToken);
                    Console.WriteLine($"[OTP-VERIFY] Fresh access token: {freshAccessToken}");
                }
                else
                {
                    Console.WriteLine($"[OTP-VERIFY] Refresh failed: {refreshResult.Error}, using existing token");
                }
            }

            // Set onboarded state before triggering CompleteLoginAsync
            if (SelectedRole == "DRIVER")
                await _userSessionService.SetOnboardedAsync(false);
            else
                await _userSessionService.SetOnboardedAsync(true);

            // Signal AppShellViewModel to build user + navigate by bootstrap state
            WeakReferenceMessenger.Default.Send(new LoginSuccessMessage(new LoginSuccessData
            {
                UserId = UserId ?? string.Empty,
                AccessToken = freshAccessToken,
                DisplayName = DisplayName ?? "Ridebase User",
                Email = UserEmail ?? string.Empty,
                PictureUrl = PictureUrl ?? string.Empty
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP verification failed");
            StatusMessage = "Something went wrong. Please try again.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ResendEmailAsync()
    {
        if (IsResending) return;

        IsResending = true;
        try
        {
            var result = await _onboardingApiClient.ResendOtpAsync();
            StatusMessage = result.IsSuccess
                ? "New code sent. Check your inbox."
                : (result.ErrorMessage ?? "Could not resend. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resend failed");
            StatusMessage = "Could not resend code. Please try again later.";
        }
        finally
        {
            IsResending = false;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("../..");
    }
}
