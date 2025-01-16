using Ridebase.Services.RestService;

namespace Ridebase.Services.Directions;

public class DirectionsService : IDirections
{
    private readonly IApiClient apiClient;

    public DirectionsService(IApiClient _apiClient)
    {
        apiClient = _apiClient;
    }

    public async Task<ApiResponse<DirectionsRoot>> GetDirections(string origin, string destination)
    {
        try
        {
            return await apiClient.GetAsync<DirectionsRoot>($"json?origin={origin}&destination={destination}&key={Constants.googleMapsApiKey}");
            
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
