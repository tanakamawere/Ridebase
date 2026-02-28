using Microsoft.Extensions.Logging;

namespace Ridebase.ViewModels.Driver;

public class DriverStatsViewModel : BaseViewModel
{
    public DriverStatsViewModel(ILogger<DriverStatsViewModel> logger)
    {
        Logger = logger;
        Logger.LogInformation("DriverStatsViewModel initialized");
    }
}
