using Ridebase.Models;
using Ridebase.Models.Ride;

namespace Ridebase.Services.Interfaces;

public interface IDriverApiClient
{
    //Method to listen for ride request from nearby riders
    public Task<DriverRideRequest> DriverRideRequestListener();
}
