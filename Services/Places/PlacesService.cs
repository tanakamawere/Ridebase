using Ridebase.Models;
using Ridebase.Services.RestService;
using System.Text.Json;

namespace Ridebase.Services.Places;

public class PlacesService : IPlaces
{
    private readonly IApiClient apiClient;

    public PlacesService(IApiClient _apiClient)
    {
        apiClient = _apiClient;
    }

    public async Task<ApiResponse<List<Place>>> GetPlacesAutocomplete(string keyword)
    {
        try
        {
            var requestBody = new
            {
                textQuery = keyword,
                pageSize = 10,
                minRating = 2
            };

            //AddHttpHeader("X-Goog-Api-Key", Constants.googlePlacesApiKey);
            //AddHttpHeader("X-Goog-FieldMask", "places.displayName,places.formattedAddress,places.location,places.id,places.types");
            //var response = await PostAsync("", requestBody);

            ////Read the response and convert it to a Root Object
            //var responseStream = await response.Content.ReadAsStreamAsync();
            //var placesRoot = await JsonSerializer.DeserializeAsync<PlacesRoot>(responseStream);

            //return placesRoot.places;

            //AddHttpHeader("X-Goog-Api-Key", Constants.googlePlacesApiKey);
            //AddHttpHeader("X-Goog-FieldMask", "places.displayName,places.formattedAddress,places.location,places.id,places.types");
            return await apiClient.PostAsync<List<Place>>("", requestBody);
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
