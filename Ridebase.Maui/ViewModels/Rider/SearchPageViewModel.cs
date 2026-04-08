using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using GoogleApi.Entities.Places.Common;
using GoogleApi.Entities.Places.Search.NearBy.Request;
using Microsoft.Extensions.Logging;
using Ridebase.Pages;
using Ridebase.Pages.Rider;
using Ridebase.Models;
using Ridebase.Services;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(CurrentLocation), "currentLocation")]
public partial class SearchPageViewModel : BaseViewModel
{
    [ObservableProperty]
    private LocationWithAddress? currentLocation;
    //Start place
    [ObservableProperty]
    private PlaceResult? startPlace;
    [ObservableProperty]
    private PlaceResult? destinationPlace;
    [ObservableProperty]
    private string startLocation = string.Empty;
    [ObservableProperty]
    private string startSearchQuery = string.Empty;
    [ObservableProperty]
    private string destinationSearchQuery = string.Empty;
    [ObservableProperty]
    public ObservableCollection<PlacePrediction> places = [];
    //Used for determining which place, start or destination is to be set
    private LocationType locationType = LocationType.Destination;
    private CancellationTokenSource? cts;

    private readonly IMapService _mapService;

    public SearchPageViewModel(IMapService mapService, ILogger<SearchPageViewModel> logger)
    {
        Title = "Search";
        Logger = logger;
        _mapService = mapService;
    }

    public async Task SearchPlaces(string keyword)
    {
        Logger.LogInformation("Searching for places with keyword: {Keyword}", keyword);
        IsBusy = true;
        Places.Clear();
        try
        {
            var results = await _mapService.GetAutocompleteAsync(keyword);

            if (results != null)
            {
                foreach (var item in results)
                {
                    Places.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching for places with keyword: {Keyword}", keyword);
            await AppShell.Current.DisplayAlertAsync("Error", "Search failed. Check your network.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnDestinationSearchQueryChanged(string value)
    {
        //Check if value is not null
        if (!string.IsNullOrEmpty(value))
            _ = DebounceSearch(value);
    }

    partial void OnStartSearchQueryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = DebounceSearch(value);
    }

    private async Task DebounceSearch(string query)
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();

        await Task.Delay(00, cts.Token);

        if (cts.IsCancellationRequested)
            return;

        await SearchPlaces(query);
    }

    [RelayCommand]
    //navigate to page to choose location on map
    public async Task ChooseLocationOnMap()
    {
        //TODO: Change to a map page and handle this
        await Shell.Current.GoToAsync(nameof(RideDetailsPage));
    }

    [RelayCommand]
    //Method to select the location from the collection view
    public async Task SelectPlace(PlacePrediction place)
    {
        if (place == null)
        {
            Logger.LogWarning("SelectPlace called with null place");
            return;
        }

        Logger.LogInformation("Place selected: {PlaceName}, Type: {LocationType}", place.MainText, locationType);

        switch (locationType)
        {
            case LocationType.Start:
                StartSearchQuery = place.MainText;
                Logger.LogInformation("Start location set to: {PlaceName}", place.MainText);
                break;

            case LocationType.Destination:
                DestinationSearchQuery = place.MainText;
                Logger.LogInformation("Destination location set to: {PlaceName}", place.MainText);
                
                if (!string.IsNullOrEmpty(StartSearchQuery))
                {
                    await GoToRideDetailsPage();
                }
                break;
        }
        Places.Clear();
    }

    [RelayCommand]
    public void EntryFocused(LocationType _locationType)
    {
        Places.Clear();
        locationType = _locationType;
    }

    //Method to go to the ride details page with the start and destination locations
    public async Task GoToRideDetailsPage()
    {
        Logger.LogInformation("Navigating to RideDetailsPage with Start: {StartSearchQuery} and Destination: {DestinationSearchQuery}", StartSearchQuery, DestinationSearchQuery);
        await Shell.Current.GoToAsync(nameof(RideDetailsPage));
    }
}

public enum LocationType
{
    Start,
    Destination
}