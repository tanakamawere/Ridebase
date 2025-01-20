using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class RideDetailsPage : ContentPage
{
	private readonly RideDetailsViewModel rideDetailsViewModel;
	public RideDetailsPage(RideDetailsViewModel _rideDetailsViewModel)
    {
        InitializeComponent();
        BindingContext = rideDetailsViewModel = _rideDetailsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (rideDetailsViewModel.Polylines.Count == 0)
        {
            rideDetailsViewModel.GetDirectionsAsync();
        }
    }
}