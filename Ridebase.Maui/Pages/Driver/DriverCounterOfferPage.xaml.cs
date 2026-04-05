namespace Ridebase.Pages.Driver;

public partial class DriverCounterOfferPage : ContentPage
{
    public DriverCounterOfferPage(ViewModels.Driver.DriverCounterOfferViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
