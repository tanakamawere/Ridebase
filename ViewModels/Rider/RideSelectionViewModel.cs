using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Pages.Rider;
using Ridebase.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(RideRequest), "rideRequest")]
public partial class RideSelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<DriverOfferSelectionModel> driversList;
    [ObservableProperty]
    private RideRequestModel rideRequest;

    public RideSelectionViewModel(IRideRealtimeService _rideRealtimeService, IRideStateStore _rideStateStore, IUserSessionService _userSessionService, ILogger<RideSelectionViewModel> logger)
    {
        Logger = logger;
        rideRealtimeService = _rideRealtimeService;
        rideStateStore = _rideStateStore;
        userSessionService = _userSessionService;
        DriversList = new ObservableCollection<DriverOfferSelectionModel>();

        rideRealtimeService.RiderOfferReceived += HandleIncomingOffer;
    }

    private void HandleIncomingOffer(DriverOfferSelectionModel offer)
    {
        App.Current?.Dispatcher.Dispatch(() => DriversList.Add(offer));
    }

    [RelayCommand]
    public async Task SelectDriver(DriverOfferSelectionModel driver)
    {
        if (driver == null)
        {
            Logger.LogWarning("SelectDriver called with null driver");
            return;
        }

        Logger.LogInformation("Driver selected: DriverId={DriverId}, OfferAmount={OfferAmount}", driver.Driver?.DriverId, driver.OfferAmount);

        //Create Driver Accept Request Object
        var driverAcceptRequest = new RideAcceptRequest
        {
            RideId = RideRequest.RideGuid.ToString("N"),
            DriverId = driver.Driver?.DriverId ?? Guid.Empty,
            RiderId = RideRequest.RiderId,
            OfferAmount = driver.OfferAmount,
            Status = RideStatus.OfferAccepted,
            StartLocation = RideRequest.StartLocation,
            DestinationLocation = RideRequest.DestinationLocation
        };

        try
        {
            Logger.LogInformation("Sending driver accept request via realtime service");
            await rideRealtimeService.AcceptOfferAsync(driverAcceptRequest);

            var session = await CreateRideSession(driver);
            rideStateStore.SetCurrentRide(session);

            await rideRealtimeService.UpdateRideStatusAsync(session.RideId, RideStatus.DriverEnRoute);
            await Shell.Current.GoToAsync(nameof(RideProgressPage));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending driver accept request");
        }
    }

    [RelayCommand]
    public void DeclineDriver(DriverOfferSelectionModel driver)
    {
        if (driver is null)
        {
            return;
        }

        DriversList.Remove(driver);
    }

    private async Task<RideSessionModel> CreateRideSession(DriverOfferSelectionModel selectedOffer)
    {
        var session = await userSessionService.GetStateAsync();

        return new RideSessionModel
        {
            RideId = RideRequest.RideGuid.ToString("N"),
            RiderId = RideRequest.RiderId,
            DriverId = selectedOffer.Driver?.DriverId ?? Guid.Empty,
            RiderName = session.FullName,
            RiderPhoneNumber = session.PhoneNumber,
            DriverName = selectedOffer.Driver?.Name ?? "Driver",
            DriverPhoneNumber = selectedOffer.Driver?.PhoneNumber ?? string.Empty,
            VehicleInfo = selectedOffer.Driver?.Vehicle ?? "Vehicle",
            StartLocation = RideRequest.StartLocation,
            DestinationLocation = RideRequest.DestinationLocation,
            RiderOfferAmount = RideRequest.OfferAmount,
            AcceptedAmount = selectedOffer.OfferAmount,
            DistanceKm = (double)selectedOffer.Distance,
            EstimatedMinutes = 10,
            Status = RideStatus.DriverEnRoute
        };
    }
}
