using Microsoft.Extensions.Logging;

namespace Ridebase.ViewModels;

public partial class DriverShellViewModel : BaseViewModel
{
    public DriverShellViewModel(ILogger<DriverShellViewModel> logger)
    {
        Logger = logger;
    }
}
