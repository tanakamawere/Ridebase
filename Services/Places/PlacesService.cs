using Ridebase.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Ridebase.Services.Places;

public class PlacesService : IPlaces
{
    private readonly HttpClient httpClient;
    public PlacesService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    public async Task<List<Place>> GetPlacesAutocomplete(string keyword)
    {
        try
        {
            var requestBody = new
            {
                textQuery = keyword,
                pageSize = 10
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            //setting headers
            httpClient.DefaultRequestHeaders.Add("X-Goog-Api-Key", Constants.googlePlacesApiKey);
            httpClient.DefaultRequestHeaders.Add("X-Goog-FieldMask", "places.displayName,places.formattedAddress,places.location,places.id,places.types");

            // Make the POST request
            var response = await httpClient.PostAsync(Constants.googlePlacesApiUrl, content);

            //Read the response and convert it to a Root Object
            //Check if it was successful
            if (response.IsSuccessStatusCode)
            {
                var responseStream = await response.Content.ReadAsStreamAsync();
                var placesRoot = await JsonSerializer.DeserializeAsync<PlacesRoot>(responseStream);

                httpClient.DefaultRequestHeaders.Clear();

                return placesRoot.places;
            }
            else
            {
                return new List<Place>();
            }
        }
        catch (Exception ex)
        {
            return new List<Place>();
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
