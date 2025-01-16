using Ridebase.Models;
using Ridebase.Services.RestService;

namespace Ridebase.Services.Places;

public interface IPlaces
{
    Task<ApiResponse<List<Place>>> GetPlacesAutocomplete(string keyword);
}
