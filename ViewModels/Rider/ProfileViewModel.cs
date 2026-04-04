using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Models.Rider;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

public partial class ProfileViewModel : BaseViewModel
{
    [ObservableProperty]
    private string riderName = "Vuka Transit";

    [ObservableProperty]
    private string riderContact = "rider@ridebase.app";

    public ObservableCollection<RiderProfileOption> Options { get; } =
    [
        new RiderProfileOption
        {
            Title = "Account Settings",
            Subtitle = "Privacy, connected apps, and account controls",
            IconGlyph = "\uf013",
            AccentColor = "#E8EDEE"
        },
        new RiderProfileOption
        {
            Title = "Payment Methods",
            Subtitle = "EcoCash, Visa, and wallet credits",
            IconGlyph = "\uf09d",
            AccentColor = "#CCE8E7"
        },
        new RiderProfileOption
        {
            Title = "Safety & Security",
            Subtitle = "Two-factor options and emergency contacts",
            IconGlyph = "\uf505",
            AccentColor = "#E8EDEE"
        },
        new RiderProfileOption
        {
            Title = "Support",
            Subtitle = "Help center, live chat, and FAQs",
            IconGlyph = "\uf059",
            AccentColor = "#E8EDEE"
        }
    ];

    public ProfileViewModel(IUserSessionService userSessionService)
    {
        this.userSessionService = userSessionService;
        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task SwitchToDriverMode()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Driver);
        await Shell.Current.GoToAsync("//DriverHome");
    }

    [RelayCommand]
    private Task GoHome() => Shell.Current.GoToAsync("//Home");

    [RelayCommand]
    private Task GoToHistory() => Shell.Current.GoToAsync(nameof(RideHistoryPage));

    [RelayCommand]
    private Task GoToWallet() => Shell.Current.GoToAsync(nameof(WalletPage));

    [RelayCommand]
    private async Task Logout()
    {
        await Shell.Current.DisplayAlertAsync("Log Out", "Logout wiring is still handled from the flyout menu.", "OK");
    }

    private async Task InitializeAsync()
    {
        var state = await userSessionService.GetStateAsync();
        if (!string.IsNullOrWhiteSpace(state.FullName))
        {
            RiderName = state.FullName;
        }

        if (!string.IsNullOrWhiteSpace(state.PhoneNumber))
        {
            RiderContact = state.PhoneNumber;
        }
    }
}
