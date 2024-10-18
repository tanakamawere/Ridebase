using Maui.Apps.Framework.Services;

namespace Ridebase.Services.Directions;

public class DirectionsService : RestServiceBase, IDirections
{
    public DirectionsService(IConnectivity connectivity) : base(connectivity)
    {
        SetBaseURL(Constants.GoogleDirectionsApiUrl);
    }

    public async Task<DirectionsRoot> GetDirections(string origin, string destination)
    {
        try
        {
            return await GetAsync<DirectionsRoot>($"json?origin={origin}&destination={destination}&key={Constants.googleMapsApiKey}");
            
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
