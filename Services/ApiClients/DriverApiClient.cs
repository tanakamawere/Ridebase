using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class DriverApiClient : IDriverApiClient
{
    private readonly IApiClient _apiClient;

    public DriverApiClient(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc />
    public Task<DriverRideRequest> DriverRideRequestListener()
        => throw new NotSupportedException(
            "Use GetPendingRideRequestsAsync for the production driver client.");

    /// <inheritdoc />
    public async Task<IEnumerable<DriverRideRequest>> GetPendingRideRequestsAsync(
        string driverId, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<IEnumerable<DriverRideRequest>>(
            $"api/driver/{driverId}/rides/pending");

        return response.IsSuccess && response.Data is not null
            ? response.Data
            : Enumerable.Empty<DriverRideRequest>();
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> AcceptRideRequestAsync(
        RideAcceptRequest request, CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<bool>("api/driver/rides/accept", request);
    }
}
