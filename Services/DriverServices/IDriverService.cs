namespace Ridebase.Services.DriverServices;

public interface IDriverService
{
    //Method to listen for ride request from nearby riders
    public Task<DriverRideRequest> DriverRideRequestListener();
}
