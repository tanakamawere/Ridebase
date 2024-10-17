
namespace Ridebase.Services.RideService;

public class RideService : IRideService
{
    private readonly HttpClient httpClient;
    public RideService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task CancelRide()
    {
        throw new NotImplementedException();
    }

    public Task GetRideDetails()
    {
        throw new NotImplementedException();
    }

    public Task GetRideStatus()
    {
        throw new NotImplementedException();
    }

    public Task RequestRide()
    {
        throw new NotImplementedException();
    }

    public Task TrackRide()
    {
        throw new NotImplementedException();
    }
}
