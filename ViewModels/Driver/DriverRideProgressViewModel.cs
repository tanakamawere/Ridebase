using Microsoft.Extensions.Logging;

namespace Ridebase.ViewModels.Driver;

[QueryProperty("currentLocation", "currentLocation")]
public class DriverRideProgressViewModel : BaseViewModel
{
    public DriverRideProgressViewModel(ILogger<DriverRideProgressViewModel> logger)
    {
        Logger = logger;
        Logger.LogInformation("DriverRideProgressViewModel initialized");
    }
}
