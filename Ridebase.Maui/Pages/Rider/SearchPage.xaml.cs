using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class SearchPage : ContentPage
{
	private readonly SearchPageViewModel searchPageViewModel;
	public SearchPage(SearchPageViewModel searchPageViewModel)
	{
		InitializeComponent();

        BindingContext = this.searchPageViewModel = searchPageViewModel;
    }
}