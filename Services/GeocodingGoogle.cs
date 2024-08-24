using Ridebase.Models;
using System.Net.Http.Json;

namespace Ridebase.Services;

public class GeocodingGoogle : IGeocodeGoogle
{
    public readonly HttpClient httpClient;

    public GeocodingGoogle()
    {
        httpClient = new();
    }

    public async Task<BaseResponse> GetLocationsAsync(string address)
    {
        string url = $"{Constants.googleMapsApiUrl}address={address}&key={Constants.googleMapsApiKey}";

        Console.WriteLine(url);

        var response = await httpClient.GetFromJsonAsync<BaseResponse>(url);

        return response;
    }

    public async Task<BaseResponse> GetPlacemarksAsync(double latitude, double longitude)
    {
        string url = $"{Constants.googleMapsApiUrl}latlng={latitude},{longitude}&key={Constants.googleMapsApiKey}";

        Console.WriteLine(url);

        var response = await httpClient.GetFromJsonAsync<BaseResponse>(url);

        return response;
    }
}
