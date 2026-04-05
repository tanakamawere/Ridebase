using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models.Ride;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(RideRequest), "rideRequest")]
public partial class RideSelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<DriverOfferSelectionModel> driversList = [];

    [ObservableProperty]
    private RideRequestModel rideRequest = new();

    public RideSelectionViewModel(
        IRideRealtimeService realtimeService,
        IRideStateStore rideStateStore,
        IUserSessionService userSessionService,
        IRideApiClient rideApiClient,
        ILogger<RideSelectionViewModel> logger)
    {
        Logger = logger;
        rideRealtimeService = realtimeService;
        this.rideStateStore = rideStateStore;
        this.userSessionService = userSessionService;
        this.rideApiClient = rideApiClient;

        rideRealtimeService.RiderOfferReceived += HandleIncomingOffer;
    }

    partial void OnRideRequestChanged(RideRequestModel value)
    {
        DriversList.Clear();
    }

    private void HandleIncomingOffer(DriverOfferSelectionModel offer)
    {
        var rideRequest = RideRequest;
        if (rideRequest is null || rideRequest.RideGuid == Guid.Empty)
        {
            return;
        }

        if (!string.Equals(offer.RideId, rideRequest.RideGuid.ToString("N"), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        App.Current?.Dispatcher.Dispatch(() =>
        {
            var existing = DriversList.FirstOrDefault(item => item.RideOfferId == offer.RideOfferId);
            if (existing is not null)
            {
                DriversList.Remove(existing);
            }

            DriversList.Add(offer);
        });
    }

    [RelayCommand]
    public async Task SelectDriver(DriverOfferSelectionModel driver)
    {
        if (driver?.Driver is null)
        {
            Logger.LogWarning("SelectDriver called with an invalid driver offer");
            return;
        }

        var acceptRequest = new RideAcceptRequest
        {
            RideId = RideRequest.RideGuid.ToString("N"),
            DriverId = driver.Driver.DriverId,
            RideOfferId = driver.RideOfferId,
            RiderId = RideRequest.RiderId,
            OfferAmount = driver.OfferAmount,
            RecommendedAmount = RideRequest.RecommendedAmount,
            Status = RideStatus.OfferAccepted,
            PickupAddress = RideRequest.StartAddress,
            DestinationAddress = RideRequest.DestinationAddress,
            StartLocation = RideRequest.StartLocation,
            DestinationLocation = RideRequest.DestinationLocation
        };

        try
        {
            await rideApiClient.SelectOffer(acceptRequest);
            await rideRealtimeService.AcceptOfferAsync(acceptRequest);

            var session = await CreateRideSession(driver);
            rideStateStore.SetCurrentRide(session);

            await Shell.Current.GoToAsync(nameof(RideProgressPage));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error accepting driver offer");
        }
    }

    [RelayCommand]
    public void DeclineDriver(DriverOfferSelectionModel driver)
    {
        if (driver is not null)
        {
            DriversList.Remove(driver);
        }
    }

    [RelayCommand]
    public async Task CancelRide()
    {
        try
        {
            await rideRealtimeService.StopAsync();

            var rideRequest = RideRequest;
            if (rideRequest is not null && rideRequest.RideGuid != Guid.Empty)
            {
                await rideApiClient.CancelRide(rideRequest.RideGuid.ToString("N"));
            }

            rideStateStore.SetCurrentRide(null);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling ride from selection screen");
        }
    }

    private async Task<RideSessionModel> CreateRideSession(DriverOfferSelectionModel selectedOffer)
    {
        var session = await userSessionService.GetStateAsync();

        return new RideSessionModel
        {
            RideId = RideRequest.RideGuid.ToString("N"),
            RiderId = RideRequest.RiderId,
            DriverId = selectedOffer.Driver?.DriverId ?? Guid.Empty,
            RiderName = string.IsNullOrWhiteSpace(session.FullName) ? "Kinetic Rider" : session.FullName,
            RiderPhoneNumber = session.PhoneNumber,
            DriverName = selectedOffer.Driver?.Name ?? "Driver",
            DriverPhoneNumber = selectedOffer.Driver?.PhoneNumber ?? string.Empty,
            VehicleInfo = selectedOffer.Driver?.Vehicle ?? "Vehicle",
            StartLocation = RideRequest.StartLocation,
            StartAddress = RideRequest.StartAddress,
            DestinationLocation = RideRequest.DestinationLocation,
            DestinationAddress = RideRequest.DestinationAddress,
            RiderOfferAmount = RideRequest.OfferAmount,
            RecommendedAmount = RideRequest.RecommendedAmount,
            AcceptedAmount = selectedOffer.OfferAmount,
            SelectedOfferId = selectedOffer.RideOfferId,
            DistanceKm = RideRequest.EstimatedDistanceKm <= 0 ? (double)selectedOffer.Distance : RideRequest.EstimatedDistanceKm,
            EstimatedMinutes = RideRequest.EstimatedMinutes,
            DriverEtaMinutes = selectedOffer.EtaToPickupMinutes,
            DriverCurrentLocation = selectedOffer.PickupLocation ?? RideRequest.StartLocation,
            DriverStatusNote = selectedOffer.IsCounterOffer ? "Counter offer accepted" : "Driver accepted your offer",
            RequestedAtUtc = RideRequest.RequestedAtUtc,
            AcceptedAtUtc = DateTimeOffset.UtcNow,
            Status = RideStatus.DriverEnRoute
        };
    }
}
