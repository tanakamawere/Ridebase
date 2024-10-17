﻿namespace Ridebase.Services.Geocoding;

public interface IGeocodeGoogle
{
    //Methods that call the Google Maps Geocoding API
    Task<BaseResponse> GetLocationsAsync(string address);
    Task<BaseResponse> GetPlacemarksAsync(double latitude, double longitude);
}