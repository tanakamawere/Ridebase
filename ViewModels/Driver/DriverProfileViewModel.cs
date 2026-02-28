using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Pages;

namespace Ridebase.ViewModels.Driver;

public partial class DriverProfileViewModel : BaseViewModel
{
    public DriverProfileViewModel(ILogger<DriverProfileViewModel> logger)
    {
        Logger = logger;
    }

    [RelayCommand]
    //Method to go to AppShell and pop the stack
    public void GoToRiderPages()
    {
        Logger.LogInformation("Switching to Rider Pages");
        try
        {
            Application.Current.OpenWindow(new Window(new AppShell()));
            Logger.LogInformation("Successfully switched to Rider Pages");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error switching to Rider Pages");
        }
    }
}