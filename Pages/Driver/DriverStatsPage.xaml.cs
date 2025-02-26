using Ridebase.ViewModels.Driver;

namespace Ridebase.Pages.Driver;

public partial class DriverStatsPage : ContentPage
{
	private readonly DriverStatsViewModel _viewModel;
    public DriverStatsPage(DriverStatsViewModel driverStatsViewModel)
	{
        BindingContext = _viewModel = driverStatsViewModel;
        InitializeComponent();
	}
}