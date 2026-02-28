using Ridebase.Services;
using Ridebase.ViewModels;

namespace Ridebase.Pages.Rider;

public partial class HomePage : ContentPage
{
	private readonly HomePageViewModel homePageViewModel;
	private readonly IKeyboardService _keyboardService;

	public HomePage(HomePageViewModel _homePageViewModel, IKeyboardService keyboardService)
	{
		InitializeComponent();

        homePageViewModel = _homePageViewModel;
        _keyboardService = keyboardService;
        BindingContext = homePageViewModel;

        _keyboardService.KeyboardStateChanged += OnKeyboardStateChanged;
    }

    private void OnKeyboardStateChanged(object? sender, bool isVisible)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isVisible &&
                homePageViewModel.CurrentSearchState == SearchState.PickingDestination)
            {
                BottomSheet.TranslationY = 0;
            }
        });
    }

    /// <summary>
    /// Focuses the correct Entry when the Border wrapper is tapped.
    /// </summary>
    private void OnPickupBorderTapped(object? sender, TappedEventArgs e)
    {
        homePageViewModel.SetActiveField("Pickup");
        PickupEntry.Focus();
    }

    private void OnDestinationBorderTapped(object? sender, TappedEventArgs e)
    {
        homePageViewModel.SetActiveField("Destination");
        DestinationEntry.Focus();
    }

    /// <summary>
    /// When a search Entry gains focus, update the active field in the ViewModel.
    /// </summary>
    private void OnSearchEntryFocused(object? sender, FocusEventArgs e)
    {
        if (sender == PickupEntry)
            homePageViewModel.SetActiveField("Pickup");
        else if (sender == DestinationEntry)
            homePageViewModel.SetActiveField("Destination");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _keyboardService.KeyboardStateChanged -= OnKeyboardStateChanged;
    }
}