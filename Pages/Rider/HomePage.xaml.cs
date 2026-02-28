using Ridebase.Services;
using Ridebase.ViewModels;

namespace Ridebase.Pages.Rider;

public partial class HomePage : ContentPage
{
	private readonly HomePageViewModel homePageViewModel;
	private readonly IKeyboardService _keyboardService;
    private SearchState _previousState;

	public HomePage(HomePageViewModel _homePageViewModel, IKeyboardService keyboardService)
	{
		InitializeComponent();

        homePageViewModel = _homePageViewModel;
        _keyboardService = keyboardService;
        BindingContext = homePageViewModel;

        _previousState = homePageViewModel.CurrentSearchState;
        homePageViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _keyboardService.KeyboardStateChanged += OnKeyboardStateChanged;
    }

    // ═════════════════════════════════════════════════════════════
    //  STATE TRANSITION ANIMATIONS
    // ═════════════════════════════════════════════════════════════

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HomePageViewModel.CurrentSearchState))
            return;

        var newState = homePageViewModel.CurrentSearchState;
        if (newState == _previousState) return;

        var from = _previousState;
        _previousState = newState;

        MainThread.BeginInvokeOnMainThread(() => AnimateStateTransition(from, newState));
    }

    private async void AnimateStateTransition(SearchState from, SearchState to)
    {
        const uint duration = 250;

        // ── Fade out floating overlays when leaving Idle ──
        if (from == SearchState.Idle && to == SearchState.PickingDestination)
        {
            // Fade out search bar + hamburger
            var fadeOut = Task.WhenAll(
                FloatingSearchBar.FadeTo(0, duration / 2, Easing.CubicIn),
                FloatingHamburger.FadeTo(0, duration / 2, Easing.CubicIn));

            // Slide bottom sheet content up
            BottomSheet.TranslationY = 60;
            BottomSheet.Opacity = 0;
            await fadeOut;

            await Task.WhenAll(
                BottomSheet.TranslateTo(0, 0, duration, Easing.CubicOut),
                BottomSheet.FadeTo(1, duration, Easing.CubicOut));
            return;
        }

        // ── Returning to Idle ──
        if (to == SearchState.Idle)
        {
            // Fade in floating overlays
            FloatingSearchBar.Opacity = 0;
            FloatingHamburger.Opacity = 0;
            FloatingSearchBar.TranslationY = -20;
            FloatingHamburger.TranslationX = -20;

            await Task.WhenAll(
                FloatingSearchBar.FadeTo(1, duration, Easing.CubicOut),
                FloatingSearchBar.TranslateTo(0, 0, duration, Easing.CubicOut),
                FloatingHamburger.FadeTo(1, duration, Easing.CubicOut),
                FloatingHamburger.TranslateTo(0, 0, duration, Easing.CubicOut));
            return;
        }

        // ── Generic transitions between non-Idle states ──
        // Get the incoming content view
        var incoming = GetStateContent(to);
        if (incoming is null) return;

        incoming.Opacity = 0;
        incoming.TranslationY = 30;
        await Task.WhenAll(
            incoming.FadeTo(1, duration, Easing.CubicOut),
            incoming.TranslateTo(0, 0, duration, Easing.CubicOut));
    }

    private VisualElement? GetStateContent(SearchState state) => state switch
    {
        SearchState.Idle => IdleContent,
        SearchState.PickingDestination => PickingDestinationContent,
        SearchState.RoutePreview => RoutePreviewContent,
        SearchState.FindingDriver => FindingDriverContent,
        _ => null
    };

    // ═════════════════════════════════════════════════════════════
    //  KEYBOARD
    // ═════════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════════
    //  ENTRY FOCUS
    // ═════════════════════════════════════════════════════════════

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

    private void OnSearchEntryFocused(object? sender, FocusEventArgs e)
    {
        if (sender == PickupEntry)
            homePageViewModel.SetActiveField("Pickup");
        else if (sender == DestinationEntry)
            homePageViewModel.SetActiveField("Destination");
    }

    // ═════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═════════════════════════════════════════════════════════════

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        homePageViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _keyboardService.KeyboardStateChanged -= OnKeyboardStateChanged;
    }
}