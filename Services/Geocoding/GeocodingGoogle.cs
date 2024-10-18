using System.Net.Http.Json;

namespace Ridebase.Services.Geocoding;

public class GeocodingGoogle : IGeocodeGoogle
{
    public readonly HttpClient httpClient;

    public GeocodingGoogle(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Ridebase.Models.Location> GetCurrentLocation()
    {
        GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
        Location location = await Geolocation.Default.GetLocationAsync(request);
        return new Models.Location()
        {
            latitude = location.Latitude,
            longitude = location.Longitude
        };
    }

    public async Task<LocationWithAddress> GetCurrentLocationWithAddressAsync()
    {
        var location = await GetCurrentLocation();
        return new LocationWithAddress()
        {
            Location = location,
            FormattedAddress = (await GetPlacemarksAsync(location.latitude, location.longitude)).results.FirstOrDefault().formatted_address
        };
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
