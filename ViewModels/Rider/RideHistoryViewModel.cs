using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models.Rider;
using Ridebase.Pages.Rider;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

public partial class RideHistoryViewModel : BaseViewModel
{
    [ObservableProperty]
    private string selectedFilter = "Completed";

    public ObservableCollection<RiderActivityItem> ActivityItems { get; } =
    [
        new RiderActivityItem
        {
            DriverRouteText = "Vuka Pro • Simba Makoni",
            TimestampText = "Oct 24, 2023 • 08:45 AM",
            PickupText = "Avondale Shopping Centre",
            DropOffText = "RG Mugabe International Airport",
            AmountText = "$24.50",
            AccentColor = "#CCE8E7"
        },
        new RiderActivityItem
        {
            DriverRouteText = "Vuka Mini • Tendai M.",
            TimestampText = "Oct 23, 2023 • 05:12 PM",
            PickupText = "Fife Avenue Market",
            DropOffText = "Mount Pleasant",
            AmountText = "$8.20",
            AccentColor = "#F6E3D7"
        },
        new RiderActivityItem
        {
            DriverRouteText = "Vuka Standard • Blessing C.",
            TimestampText = "Oct 23, 2023 • 11:30 AM",
            PickupText = "Sam Levy's Village",
            DropOffText = "Harare Sports Club",
            AmountText = "$12.00",
            AccentColor = "#E8EDEE"
        }
    ];

    [RelayCommand]
    private void SetFilter(string filter)
    {
        SelectedFilter = filter;
    }

    [RelayCommand]
    private Task GoHome() => Shell.Current.GoToAsync("//Home");

    [RelayCommand]
    private Task GoToWallet() => Shell.Current.GoToAsync(nameof(WalletPage));

    [RelayCommand]
    private Task GoToProfile() => Shell.Current.GoToAsync(nameof(ProfilePage));
}
