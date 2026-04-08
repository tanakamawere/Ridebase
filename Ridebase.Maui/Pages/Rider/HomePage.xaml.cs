using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling.Layers;
using Ridebase.Helpers;
using Ridebase.Models;
using Ridebase.Services;
using Ridebase.ViewModels;

namespace Ridebase.Pages.Rider;

public partial class HomePage : ContentPage
{
	private readonly HomePageViewModel homePageViewModel;
	private readonly IKeyboardService _keyboardService;
    private SearchState _previousState;
    private bool _isKeyboardVisible;
    private bool _isDraggingBottomSheet;
    private double _dragStartHeight;
    private double? _manualSnapHeight;

	public HomePage(HomePageViewModel _homePageViewModel, IKeyboardService keyboardService)
	{
		InitializeComponent();

        homePageViewModel = _homePageViewModel;
        _keyboardService = keyboardService;
        BindingContext = homePageViewModel;

        _previousState = homePageViewModel.CurrentSearchState;
        homePageViewModel.PropertyChanged += OnViewModelPropertyChanged;
        homePageViewModel.OnRequestMapUpdate += OnRequestMapUpdate; // New bridge
        _keyboardService.KeyboardStateChanged += OnKeyboardStateChanged;
        SizeChanged += OnPageSizeChanged;

        InitializeOsmMap();
    }

    private void InitializeOsmMap()
    {
        var map = new Mapsui.Map();
        
        // Load self-hosted tiles
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(), 
            Constants.OsmTileUrl, 
            name: "Self-Hosted OSM");
            
        var tileLayer = new TileLayer(tileSource) { Name = "Self-Hosted OSM" };
        map.Layers.Add(tileLayer);

        // Center on Zimbabwe
        var (x, y) = SphericalMercator.FromLonLat(31.05, -17.82);
        map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolution: 200);

        MapControl.Map = map;
    }

    private async void OnRequestMapUpdate(object? sender, MapUpdateEventArgs e)
    {
        var mapControl = MapControl;
        if (mapControl == null) return;

        switch (e.Type)
        {
            case MapUpdateType.Camera:
                var (x, y) = SphericalMercator.FromLonLat(e.Longitude, e.Latitude);
                mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), e.Zoom > 0 ? e.Zoom : 38);
                break;
            case MapUpdateType.Route:
                if (!string.IsNullOrEmpty(e.RoutePolyline))
                {
                    // Draw polyline logic here (Simplified for PoC)
                    // In a full implementation, we'd use Mapsui.Layers.GenericLayer
                }
                break;
            case MapUpdateType.Clear:
                // Special case: ViewModel wants current map center for selection
                var centerX = MapControl.Map.Navigator.Viewport.CenterX;
                var centerY = MapControl.Map.Navigator.Viewport.CenterY;
                var lonLat = SphericalMercator.ToLonLat(centerX, centerY);
                await homePageViewModel.HandleMapSelectionCallback(lonLat.lat, lonLat.lon);
                break;
        }
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

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _manualSnapHeight = null;
            UpdateBottomSheetLayout();
            AnimateStateTransition(from, newState);
        });
    }

    private async void AnimateStateTransition(SearchState from, SearchState to)
    {
        const uint duration = 250;

        // ── Fade out floating overlays when leaving Idle ──
        if (from == SearchState.Idle && to == SearchState.PickingDestination)
        {
            BottomSheet.TranslationY = 60;
            BottomSheet.Opacity = 0;
            // await fadeOut;

            await Task.WhenAll(
                BottomSheet.TranslateToAsync(0, 0, duration, Easing.CubicOut),
                BottomSheet.FadeToAsync(1, duration, Easing.CubicOut));
            return;
        }

        // ── Returning to Idle ──
        if (to == SearchState.Idle)
        {
            // Transition back to Idle if needed
            return;
        }

        // ── Generic transitions between non-Idle states ──
        // Get the incoming content view
        var incoming = GetStateContent(to);
        if (incoming is null) return;

        incoming.Opacity = 0;
        incoming.TranslationY = 30;
        await Task.WhenAll(
            incoming.FadeToAsync(1, duration, Easing.CubicOut),
            incoming.TranslateToAsync(0, 0, duration, Easing.CubicOut));
    }

    private VisualElement? GetStateContent(SearchState state) => state switch
    {
        //SearchState.Idle => IdleContent,
        //SearchState.PickingDestination => PickingDestinationContent,
        //SearchState.PinningLocation => PinningLocationContent,
        //SearchState.RoutePreview => RoutePreviewContent,
        //SearchState.FindingDriver => FindingDriverContent,
        _ => null
    };

    // ═════════════════════════════════════════════════════════════
    //  KEYBOARD
    // ═════════════════════════════════════════════════════════════

    private void OnKeyboardStateChanged(object? sender, bool isVisible)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isKeyboardVisible = isVisible;
            if (isVisible &&
                homePageViewModel.CurrentSearchState == SearchState.PickingDestination)
            {
                //BottomSheet.TranslationY = 0;
            }

            UpdateBottomSheetLayout();
        });
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        UpdateBottomSheetLayout();
    }

    private void UpdateBottomSheetLayout()
    {
        if (Height <= 0)
        {
            return;
        }

        var pageHeight = Height;
        var safePadding = 20d;
        var maximumHeight = pageHeight - safePadding;
        var targetHeight = _manualSnapHeight ?? GetDefaultSheetHeight(pageHeight);

        targetHeight = Math.Max(targetHeight, 220);
        targetHeight = Math.Min(targetHeight, maximumHeight);

        if (_isDraggingBottomSheet)
        {
            BottomSheet.HeightRequest = targetHeight;
            BottomSheet.MaximumHeightRequest = maximumHeight;
            return;
        }

        BottomSheet.HeightRequest = targetHeight;
        BottomSheet.MaximumHeightRequest = maximumHeight;
    }

    private double GetDefaultSheetHeight(double pageHeight) => homePageViewModel.CurrentSearchState switch
    {
        SearchState.Idle => Math.Min(pageHeight * 0.34, 300),
        SearchState.PickingDestination => _isKeyboardVisible ? pageHeight - 20d : pageHeight * 0.84,
        SearchState.RoutePreview => Math.Min(pageHeight * 0.46, 410),
        SearchState.PinningLocation => Math.Min(pageHeight * 0.54, 470),
        SearchState.FindingDriver => Math.Min(pageHeight * 0.32, 300),
        _ => pageHeight * 0.45
    };

    private IReadOnlyList<double> GetSnapPoints(double pageHeight)
    {
        var maximumHeight = pageHeight - 20d;
        return homePageViewModel.CurrentSearchState switch
        {
            SearchState.Idle => [Math.Min(pageHeight * 0.28, 250), Math.Min(pageHeight * 0.34, 300), Math.Min(pageHeight * 0.42, 360)],
            SearchState.PickingDestination => [Math.Min(pageHeight * 0.62, 520), Math.Min(pageHeight * 0.74, 640), maximumHeight],
            SearchState.RoutePreview => [Math.Min(pageHeight * 0.38, 340), Math.Min(pageHeight * 0.46, 410), Math.Min(pageHeight * 0.58, 500)],
            SearchState.PinningLocation => [Math.Min(pageHeight * 0.42, 380), Math.Min(pageHeight * 0.54, 470), Math.Min(pageHeight * 0.68, 580)],
            SearchState.FindingDriver => [Math.Min(pageHeight * 0.28, 260), Math.Min(pageHeight * 0.32, 300), Math.Min(pageHeight * 0.4, 360)],
            _ => [GetDefaultSheetHeight(pageHeight)]
        };
    }

    private void OnBottomSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (Height <= 0)
        {
            return;
        }

        var maxHeight = Height - 20d;
        var minHeight = 220d;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDraggingBottomSheet = true;
                _dragStartHeight = BottomSheet.HeightRequest > 0 ? BottomSheet.HeightRequest : GetDefaultSheetHeight(Height);
                BottomSheetScrollView.InputTransparent = true;
                break;

            case GestureStatus.Running:
                var nextHeight = _dragStartHeight - e.TotalY;
                nextHeight = Math.Clamp(nextHeight, minHeight, maxHeight);
                BottomSheet.HeightRequest = nextHeight;
                break;

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                _isDraggingBottomSheet = false;
                BottomSheetScrollView.InputTransparent = false;

                var currentHeight = BottomSheet.HeightRequest > 0 ? BottomSheet.HeightRequest : GetDefaultSheetHeight(Height);
                var snapTarget = GetSnapPoints(Height)
                    .OrderBy(point => Math.Abs(point - currentHeight))
                    .First();

                _manualSnapHeight = snapTarget;
                AnimateBottomSheetHeight(currentHeight, snapTarget);
                break;
        }
    }

    private void AnimateBottomSheetHeight(double from, double to)
    {
        var animation = new Animation(
            callback: value => BottomSheet.HeightRequest = value,
            start: from,
            end: to,
            easing: Easing.CubicOut);

        animation.Commit(
            owner: this,
            name: "BottomSheetSnap",
            rate: 16,
            length: 220,
            finished: (_, _) => BottomSheet.HeightRequest = to);
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
    //  SEARCH (TEST OSM)
    // ═════════════════════════════════════════════════════════════

    private async void OnTestOsmTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OsmTestPage));
    }

    // ═════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═════════════════════════════════════════════════════════════

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        homePageViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _keyboardService.KeyboardStateChanged -= OnKeyboardStateChanged;
        SizeChanged -= OnPageSizeChanged;
        this.AbortAnimation("BottomSheetSnap");
    }
}
