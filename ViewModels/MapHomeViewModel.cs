using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleApi;
using Ridebase.Models;
using Ridebase.Services.Places;
using Ridebase.Services.RideService;

namespace Ridebase.ViewModels;

public partial class MapHomeViewModel : BaseViewModel
{
    [ObservableProperty]
    private string searchQuery;
    [ObservableProperty]
    private List<Place> placesList = new();

    //Create ride root and add properties to it to send to the backend
    public RideRoot rideRoot { get; set; } = new();

    public MapHomeViewModel(IPlaces places)
    {
        Title = "Map Page";
        placesApi = places;
    }

    //Search for places using Google Places API
    [RelayCommand]
    public async Task SearchPlaces()
    {
        try
        {
            IsBusy = true;

            PlacesList.Clear();

            PlacesList = await placesApi.GetPlacesAutocomplete(SearchQuery);
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
