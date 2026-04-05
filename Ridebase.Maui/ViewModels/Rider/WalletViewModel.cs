using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models.Rider;
using Ridebase.Pages.Rider;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

public partial class WalletViewModel : BaseViewModel
{
    [ObservableProperty]
    private string accountHolder = "Kinetic Rider";

    public string TotalBalance => "$1,284.50";
    public string MonthlyEarnings => "+$450.20";
    public string MonthlySpend => "-$120.45";

    public ObservableCollection<RiderWalletTransaction> Transactions { get; } =
    [
        new RiderWalletTransaction
        {
            Title = "Ride: Avondale to CBD",
            TimestampText = "Today, 08:15 AM",
            AmountText = "-$12.00",
            StatusText = "COMPLETED",
            AccentColor = "#E8EDEE",
            IconGlyph = "\uf1b9"
        },
        new RiderWalletTransaction
        {
            Title = "Daily Fleet Earnings",
            TimestampText = "Yesterday",
            AmountText = "+$84.50",
            StatusText = "RECEIVED",
            AccentColor = "#CCE8E7",
            IconGlyph = "\u0024"
        },
        new RiderWalletTransaction
        {
            Title = "Gold Tier Renewal",
            TimestampText = "Mar 24, 2024",
            AmountText = "-$29.99",
            StatusText = "RENEWAL",
            AccentColor = "#F6E3D7",
            IconGlyph = "\uf2f1"
        },
        new RiderWalletTransaction
        {
            Title = "AddFunds: EcoCash",
            TimestampText = "Mar 22, 2024",
            AmountText = "+$50.00",
            StatusText = "SUCCESS",
            AccentColor = "#E8EDEE",
            IconGlyph = "\uf0ed"
        }
    ];

    [RelayCommand]
    private Task GoHome() => Shell.Current.GoToAsync("//Home");

    [RelayCommand]
    private Task GoToHistory() => Shell.Current.GoToAsync(nameof(RideHistoryPage));

    [RelayCommand]
    private Task GoToProfile() => Shell.Current.GoToAsync(nameof(ProfilePage));

    [RelayCommand]
    private async Task AddFunds()
    {
        await Shell.Current.DisplayAlertAsync("Wallet", "Wallet top-up flow will plug in here next.", "OK");
    }

    [RelayCommand]
    private async Task Transfer()
    {
        await Shell.Current.DisplayAlertAsync("Wallet", "Transfer flow will plug in here next.", "OK");
    }
}
