using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;

namespace Ridebase.Services.ApiClients;

public class MockRideApiClient : IRideApiClient
{
    public Task<ApiResponse<string>> CancelRide(string rideId)
    {
        return Task.FromResult(new ApiResponse<string>
        {
            IsSuccess = true,
            Data = rideId,
            StatusCode = 200
        });
    }

    public Task<ApiResponse<RideSessionModel>> GetRideDetails(string rideId)
    {
        return Task.FromResult(new ApiResponse<RideSessionModel>
        {
            IsSuccess = true,
            Data = new RideSessionModel
            {
                RideId = rideId,
                StartAddress = "128 Samora Machel, Harare",
                DestinationAddress = "RG Mugabe Intl Airport, Harare",
                Status = RideStatus.DriverEnRoute
            },
            StatusCode = 200
        });
    }

    public Task<ApiResponse<RideStatus>> GetRideStatus(string rideId)
    {
        return Task.FromResult(new ApiResponse<RideStatus>
        {
            IsSuccess = true,
            Data = RideStatus.DriverEnRoute,
            StatusCode = 200
        });
    }

    public Task<ApiResponse<RideRequestResponseModel>> RequestRide(RideRequestModel rideRequest)
    {
        var distance = CalculateDistance(rideRequest.StartLocation, rideRequest.DestinationLocation);
        var eta = Math.Max(4, (int)Math.Round(distance * 2.5));

        return Task.FromResult(new ApiResponse<RideRequestResponseModel>
        {
            IsSuccess = true,
            Data = new RideRequestResponseModel
            {
                RideRequestId = rideRequest.RideGuid.ToString("N"),
                RideStatus = RideStatus.Requested,
                RideDistance = distance,
                EstimatedWaitTime = eta
            },
            StatusCode = 200
        });
    }

    public Task<ApiResponse<Ridebase.Models.Location>> TrackRide(string rideId)
    {
        return Task.FromResult(new ApiResponse<Ridebase.Models.Location>
        {
            IsSuccess = true,
            Data = new Ridebase.Models.Location { latitude = -17.8252, longitude = 31.0335 },
            StatusCode = 200
        });
    }

    public Task<ApiResponse<RideSessionModel>> SelectOffer(RideAcceptRequest acceptRequest)
    {
        return Task.FromResult(new ApiResponse<RideSessionModel>
        {
            IsSuccess = true,
            Data = new RideSessionModel
            {
                RideId = acceptRequest.RideId,
                RiderId = acceptRequest.RiderId,
                DriverId = acceptRequest.DriverId,
                SelectedOfferId = acceptRequest.RideOfferId,
                StartLocation = acceptRequest.StartLocation,
                DestinationLocation = acceptRequest.DestinationLocation,
                StartAddress = acceptRequest.PickupAddress,
                DestinationAddress = acceptRequest.DestinationAddress,
                RiderOfferAmount = acceptRequest.OfferAmount,
                RecommendedAmount = acceptRequest.RecommendedAmount,
                AcceptedAmount = acceptRequest.OfferAmount,
                DriverEtaMinutes = 6,
                Status = RideStatus.DriverEnRoute
            },
            StatusCode = 200
        });
    }

    public Task<ApiResponse<string>> SubmitDriverSos(DriverSosRequest sosRequest)
    {
        return Task.FromResult(new ApiResponse<string>
        {
            IsSuccess = true,
            Data = sosRequest.RideId,
            StatusCode = 200
        });
    }

    public Task<ApiResponse<string>> SubmitRating(RideRatingRequest ratingRequest)
    {
        return Task.FromResult(new ApiResponse<string>
        {
            IsSuccess = true,
            Data = ratingRequest.RideId,
            StatusCode = 200
        });
    }

    private static double CalculateDistance(Ridebase.Models.Location start, Ridebase.Models.Location destination)
    {
        if (start is null || destination is null)
        {
            return 0;
        }

        var latDelta = start.latitude - destination.latitude;
        var lonDelta = start.longitude - destination.longitude;
        var kmPerDegree = 111d;
        return Math.Sqrt(latDelta * latDelta + lonDelta * lonDelta) * kmPerDegree;
    }
}
