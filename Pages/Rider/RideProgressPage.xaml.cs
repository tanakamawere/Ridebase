namespace Ridebase.Pages.Rider;

public partial class RideProgressPage : ContentPage
{
	public RideProgressPage(ViewModels.Rider.RideProgressViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}