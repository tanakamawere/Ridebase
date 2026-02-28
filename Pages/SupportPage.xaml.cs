namespace Ridebase.Pages;

public partial class SupportPage : ContentPage
{
    public SupportPage()
    {
        InitializeComponent();
    }

    // ── Ride Issues ──────────────────────────────────────
    private async void OnRideProblemTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Ride Problem",
               "Describe the issue with your ride and we'll investigate.", "OK");

    private async void OnAccidentTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Accident or Safety Issue",
               "If you are in danger, call emergency services immediately.\n\n" +
               "Otherwise, tap OK and we'll connect you with our safety team.", "OK");

    private async void OnLostItemTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Lost Item",
               "We'll help you get in touch with your driver to recover your item.", "OK");

    // ── Payments & Pricing ───────────────────────────────
    private async void OnOverchargedTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Fare Dispute",
               "We'll review the fare and adjust it if necessary.", "OK");

    private async void OnPaymentIssueTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Payment Issue",
               "Check your payment method or contact support for assistance.", "OK");

    // ── Account & Safety ─────────────────────────────────
    private async void OnAccountIssueTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Account Issue",
               "We can help with login problems, profile updates, and verification.", "OK");

    private async void OnSafetyConcernTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Safety Concern",
               "Your safety is our priority. Report any concern and we'll act on it.", "OK");

    // ── General ──────────────────────────────────────────
    private async void OnHowItWorksTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("How Ridebase Works",
               "1. Set your pickup & destination\n" +
               "2. Offer your fare or accept a driver's price\n" +
               "3. Ride safely and pay when you arrive", "Got it");

    private async void OnLegalTapped(object? sender, EventArgs e)
        => await Launcher.Default.OpenAsync("https://ridebase.app/legal");

    // ── Contact Us ───────────────────────────────────────
    private async void OnLiveChatTapped(object? sender, EventArgs e)
        => await Shell.Current.DisplayAlert("Live Chat",
               "Live chat support will be available soon!", "OK");

    private async void OnEmailUsTapped(object? sender, EventArgs e)
    {
        if (Email.Default.IsComposeSupported)
        {
            await Email.Default.ComposeAsync(
                "Support Request",
                string.Empty,
                "support@ridebase.app");
        }
        else
        {
            await Shell.Current.DisplayAlert("Email",
                "Email is not supported on this device. " +
                "Please contact us at support@ridebase.app", "OK");
        }
    }
}