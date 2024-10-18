namespace Ridebase.Services.Geocoding;

public interface IGeocodeGoogle
{
    //Methods that call the Google Maps Geocoding API
    Task<BaseResponse> GetLocationsAsync(string address);
    Task<BaseResponse> GetPlacemarksAsync(double latitude, double longitude);
    //Method to get currect lat and long i.e. geolocation
    Task<Models.Location> GetCurrentLocation();
    //Method to return current location and its name
    Task<LocationWithAddress> GetCurrentLocationWithAddressAsync();
}
