using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Models.Ride;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public partial class DriverDashboardViewModel : BaseViewModel
{
    public DriverDashboardViewModel()
    {
    }

    [ObservableProperty]
    private ObservableCollection<RideRequestModel> rideRequests;

    [ObservableProperty]
    private bool isOnline;
    [ObservableProperty]
    private string onlineStatusText;

    [RelayCommand]
    private void ToggleOnlineStatus()
    {
        IsOnline = !IsOnline;
        OnlineStatusText = IsOnline ? "Go Offline" : "Go Online";
    }
}
