namespace Ridebase.Pages.Driver;

public partial class DriverRideProgressPage : ContentPage
{
	public DriverRideProgressPage(ViewModels.Driver.DriverRideProgressViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}