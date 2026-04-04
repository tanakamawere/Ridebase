namespace Ridebase.Pages.Driver;

public partial class DriverSosPage : ContentPage
{
    public DriverSosPage(ViewModels.Driver.DriverSosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
