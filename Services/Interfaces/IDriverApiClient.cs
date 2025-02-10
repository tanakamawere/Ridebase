using Ridebase.Models;

namespace Ridebase.Services.Interfaces;

public interface IDriverApiClient
{
    //Method to listen for ride request from nearby riders
    public Task<DriverRideRequest> DriverRideRequestListener();
}
