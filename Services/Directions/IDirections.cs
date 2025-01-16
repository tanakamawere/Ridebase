using Ridebase.Services.RestService;

namespace Ridebase.Services.Directions;

public interface IDirections
{
    //Get directions
    //Params
    //origin: The starting point for the directions query. Can be an Id or longlat coordinates without a space
    Task<ApiResponse<DirectionsRoot>> GetDirections(string origin, string destination);
}
