using Ridebase.ViewModels.Driver;

namespace Ridebase.Pages.Driver;

public partial class DriverDashboardPage : ContentPage
{
	public DriverDashboardPage(DriverDashboardViewModel _vm)
	{
		BindingContext = _vm;
		InitializeComponent();
	}
}
