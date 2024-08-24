using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MvvmHelpers;
using Ridebase.Models;
using Ridebase.Services;

namespace Ridebase.ViewModels;

public partial class ChooseGoToLocationViewModel : BaseViewModel
{
    public ObservableRangeCollection<Result> Locations { get; set; } = [];

    [ObservableProperty]
    private string goToLocation = "";
    private readonly IGeocodeGoogle geocodeGoogle;
    private readonly IPopupService popupService;
    public ChooseGoToLocationViewModel(IGeocodeGoogle geocode, IPopupService service)
    {
        geocodeGoogle = geocode;
        popupService = service;
    }

    [RelayCommand]
    async Task GetLocations(string text)
    {
        try
        {
            BaseResponse locations = await geocodeGoogle.GetLocationsAsync(text);
            Locations.Clear();
            Locations.AddRange(locations.results);
        }
        catch (Exception ex)
        {
            // Unable to get location
            Console.WriteLine(ex);
        }
    }
    async partial void OnGoToLocationChanged(string value)
    {
        await GetLocations(value);
    }
}
