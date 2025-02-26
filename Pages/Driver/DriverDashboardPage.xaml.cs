using Ridebase.ViewModels.Driver;

namespace Ridebase.Pages.Driver;

public partial class DriverDashboardPage : ContentPage
{
	private readonly DriverDashboardViewModel driverDashboardView;
	public DriverDashboardPage(DriverDashboardViewModel _vm)
	{
		BindingContext = driverDashboardView = _vm;
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
		bottomSheet.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
    }
}