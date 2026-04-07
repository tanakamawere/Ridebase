using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;
using System.Timers;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingOtpViewModel : BaseViewModel
{
    private System.Timers.Timer resendTimer;
    private int resendSeconds = 60;

    [ObservableProperty]
    private string otpCode = string.Empty;

    [ObservableProperty]
    private string resendText = "Resend in 60s";

    [ObservableProperty]
    private bool canResend = false;

    [ObservableProperty]
    private string userEmail = string.Empty;

    public OnboardingOtpViewModel(IOnboardingApiClient _onboardingApiClient, IUserSessionService _userSessionService)
    {
        onboardingApiClient = _onboardingApiClient;
        userSessionService = _userSessionService;
        Title = "Verify Email";
        _ = InitializeAsync();
        SetupTimer();
        StartResendTimer();
    }

    private async Task InitializeAsync()
    {
        var state = await userSessionService.GetStateAsync();
        UserEmail = state.Email;
    }

    private void SetupTimer()
    {
        resendTimer = new System.Timers.Timer(1000);
        resendTimer.Elapsed += OnTimerElapsed;
    }

    private void StartResendTimer()
    {
        CanResend = false;
        resendSeconds = 60;
        ResendText = $"Resend in {resendSeconds}s";
        resendTimer.Start();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            resendSeconds--;
            if (resendSeconds <= 0)
            {
                resendTimer.Stop();
                CanResend = true;
                ResendText = "Resend Code";
            }
            else
            {
                ResendText = $"Resend in {resendSeconds}s";
            }
        });
    }

    [RelayCommand]
    public async Task VerifyAsync()
    {
        if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Length < 6)
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Please enter the 6-digit code sent to your email.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var response = await onboardingApiClient.VerifyEmailOtpAsync(OtpCode);
            if (response.IsSuccess)
            {
                var state = await userSessionService.GetStateAsync();
                
                if (state.Role == AppUserRole.Driver)
                {
                    await userSessionService.SetOnboardedAsync(false);
                    await Shell.Current.GoToAsync(nameof(OnboardingDriverPage));
                }
                else
                {
                    await userSessionService.SetOnboardedAsync(true);
                    await Shell.Current.GoToAsync("//Home");
                }
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Verification Failed", response.ErrorMessage ?? "Invalid or expired code.", "OK");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "OTP verification failed");
            await Shell.Current.DisplayAlertAsync("Error", "Unable to verify code right now. Please try again.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ResendOtpAsync()
    {
        if (!CanResend) return;

        IsBusy = true;
        try
        {
            var response = await onboardingApiClient.ResendOtpAsync();
            if (response.IsSuccess)
            {
                StartResendTimer();
                await Shell.Current.DisplayAlertAsync("Sent", "A new code has been sent to your email.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", response.ErrorMessage ?? "Unable to resend code.", "OK");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Resend OTP failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task BackToSignInAsync()
    {
        // For now, we clear the session and go back to home where the Login button is.
        await userSessionService.ClearSessionAsync();
        await Shell.Current.GoToAsync("//Home");
    }
}
