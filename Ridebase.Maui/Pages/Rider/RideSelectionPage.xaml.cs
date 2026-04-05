using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class RideSelectionPage : ContentPage
{
	private readonly RideSelectionViewModel rideSelectionViewModel;
	public RideSelectionPage(RideSelectionViewModel rideSelectionViewModel)
	{
		InitializeComponent();
		BindingContext = this.rideSelectionViewModel = rideSelectionViewModel;
	}
}