using Ridebase.Models;

namespace Ridebase.Services.Places;

public interface IPlaces
{
    //Search for places using Google Places API
    Task SearchPlaces(string keyword);
    //Place details using Google Places API
    Task GetPlaceDetails(string keyword);
    //Get place photos using Google Places API
    Task GetPlacePhotos(string keyword);
    //Get place autocomplete using Google Places API
    Task<List<Place>> GetPlacesAutocomplete(string keyword);
    //Get place nearby search using Google Places API
    Task GetPlaceNearbySearch(string keyword);
    //Get place text search using Google Places API
    Task GetPlaceTextSearch(string keyword);
}
