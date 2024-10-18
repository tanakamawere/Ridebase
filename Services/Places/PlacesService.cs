using Ridebase.Models;
using System.Text.Json;
using Maui.Apps.Framework.Services;

namespace Ridebase.Services.Places;

public class PlacesService : RestServiceBase, IPlaces
{
    public PlacesService(IConnectivity connectivity) : base(connectivity)
    {
        SetBaseURL(Constants.googlePlacesApiUrl);
    }

    public async Task<List<Place>> GetPlacesAutocomplete(string keyword)
    {
        try
        {
            var requestBody = new
            {
                textQuery = keyword,
                pageSize = 10,
                minRating = 2
            };

            AddHttpHeader("X-Goog-Api-Key", Constants.googlePlacesApiKey);
            AddHttpHeader("X-Goog-FieldMask", "places.displayName,places.formattedAddress,places.location,places.id,places.types");
            var response = await PostAsync("", requestBody);

            //Read the response and convert it to a Root Object
            var responseStream = await response.Content.ReadAsStreamAsync();
            var placesRoot = await JsonSerializer.DeserializeAsync<PlacesRoot>(responseStream);

            return placesRoot.places;
        }
        catch (Exception ex)
        {
            return [];
        }
    }

    public Task GetPlaceDetails(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task GetPlaceNearbySearch(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task GetPlacePhotos(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task GetPlaceTextSearch(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task SearchPlaces(string keyword)
    {
        throw new NotImplementedException();
    }
}
