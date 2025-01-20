using Ridebase.Pages.Driver;
using Ridebase.ViewModels;

namespace Ridebase.Pages;

public partial class DriverShell : Shell
{
	public DriverShell()
	{
		InitializeComponent();
        BindingContext = Services.ServiceProvider.GetService<DriverShellViewModel>();

		Routing.RegisterRoute(nameof(DriverDashboardPage), typeof(DriverDashboardPage));
        Routing.RegisterRoute(nameof(DriverProfilePage), typeof(DriverProfilePage));
        Routing.RegisterRoute(nameof(DriverRideProgressPage), typeof(DriverRideProgressPage));
    }
}