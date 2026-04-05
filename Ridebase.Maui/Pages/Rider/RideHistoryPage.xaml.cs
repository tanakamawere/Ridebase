using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class RideHistoryPage : ContentPage
{
    public RideHistoryPage(RideHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
