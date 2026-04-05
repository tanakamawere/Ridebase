using Ridebase.ViewModels.Driver;

namespace Ridebase.Pages.Driver;

public partial class DriverProfilePage : ContentPage
{
	private readonly DriverProfileViewModel driverProfileViewModel;
	public DriverProfilePage(DriverProfileViewModel driverProfileView)
	{
		InitializeComponent();

		BindingContext = driverProfileViewModel = driverProfileView;
	}
}