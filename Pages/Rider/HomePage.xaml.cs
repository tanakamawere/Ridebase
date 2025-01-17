using Ridebase.ViewModels;

namespace Ridebase.Pages.Rider;

public partial class HomePage : ContentPage
{
	private readonly HomePageViewModel homePageViewModel;
	public HomePage(HomePageViewModel _homePageViewModel)
	{
		InitializeComponent();

        homePageViewModel = _homePageViewModel;
        BindingContext = homePageViewModel;
    }
}