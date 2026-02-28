using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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

    public DriverDashboardViewModel(ILogger<DriverDashboardViewModel> logger)
    {
        Logger = logger;
    }
    partial void OnIsOnlineChanged(bool oldValue, bool newValue)
    {
        Logger.LogInformation("Driver online status changed from {OldValue} to {NewValue}", oldValue, newValue);
        OnlineStatusText = newValue ? "Currently Online" : "You are offline";
    }

    //Method to go to ride in progress page
    [RelayCommand]
    public async Task GoToRideInProgress()
    {
        Logger.LogInformation("Navigating to Ride In Progress page");
        try
        {
            await Shell.Current.GoToAsync(nameof(DriverRideProgressPage), true, new Dictionary<string, object>
            {
                {"currentLocation", "something" }
            });
            Logger.LogInformation("Successfully navigated to Ride In Progress page");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to Ride In Progress page");
        }
    }
}
