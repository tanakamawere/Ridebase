namespace Ridebase.Pages.Rider;

public partial class RideEndedPage : ContentPage
{
	public RideEndedPage(ViewModels.Rider.RideEndedViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
