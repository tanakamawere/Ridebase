using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages.Driver;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public partial class DriverDashboardViewModel : BaseViewModel
{

    [ObservableProperty]
    private ObservableCollection<RideRequestModel> rideRequests;

    [ObservableProperty]
    private bool isOnline = false;
    [ObservableProperty]
    private string onlineStatusText = "You are offline";

    public DriverDashboardViewModel()
    {
    }
    partial void OnIsOnlineChanged(bool oldValue, bool newValue)
    {
        OnlineStatusText = newValue ? "Currently Online" : "You are offline";
    }

    //Method to go to ride in progress page
    [RelayCommand]
    public async Task GoToRideInProgress()
    {
        await Shell.Current.GoToAsync(nameof(DriverRideProgressPage), true, new Dictionary<string, object>
            {
                {"currentLocation", "something" }
            });
    }
}
