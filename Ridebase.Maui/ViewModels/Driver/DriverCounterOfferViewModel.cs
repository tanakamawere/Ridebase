using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Driver;

[QueryProperty(nameof(RideRequest), "rideRequest")]
public partial class DriverCounterOfferViewModel : BaseViewModel
{
    [ObservableProperty]
    private RideRequestModel rideRequest = new();

    [ObservableProperty]
    private decimal counterAmount;

    [ObservableProperty]
    private string riderSummary = "Rider trip";

    [ObservableProperty]
    private string routeSummary = "Pickup to destination";

    [ObservableProperty]
    private string guidanceText = "Set a fair amount before submitting your counteroffer.";

    public DriverCounterOfferViewModel(
        ILogger<DriverCounterOfferViewModel> logger,
        IUserSessionService userSessionService,
        IRideRealtimeService rideRealtimeService)
    {
        Logger = logger;
        this.userSessionService = userSessionService;
        this.rideRealtimeService = rideRealtimeService;
        Title = "Counter Offer";
    }

    partial void OnRideRequestChanged(RideRequestModel value)
    {
        CounterAmount = value.RecommendedAmount > 0 ? value.RecommendedAmount : value.OfferAmount + 1m;
        RiderSummary = string.IsNullOrWhiteSpace(value.RiderName)
            ? "Incoming rider request"
            : $"{value.RiderName} is requesting this trip";
        RouteSummary = $"{value.StartAddress} -> {value.DestinationAddress}";
        GuidanceText = value.RecommendedAmount > 0
            ? $"Recommended fare is ${value.RecommendedAmount:F2}. Rider offered ${value.OfferAmount:F2}."
            : $"Rider offered ${value.OfferAmount:F2}. Set the amount you'd like to propose.";
    }

    [RelayCommand]
    private void IncreaseAmount()
    {
        CounterAmount += 0.50m;
    }

    [RelayCommand]
    private void DecreaseAmount()
    {
        if (CounterAmount > 0.50m)
        {
            CounterAmount -= 0.50m;
        }
    }

    [RelayCommand]
    private async Task SubmitCounterOffer()
    {
        if (RideRequest.RideGuid == Guid.Empty)
        {
            return;
        }

        try
        {
            var session = await userSessionService.GetStateAsync();
            await rideRealtimeService.SubmitDriverOfferAsync(new DriverOfferSelectionModel
            {
                RideOfferId = Guid.NewGuid(),
                RideId = RideRequest.RideGuid.ToString("N"),
                OfferAmount = decimal.Round(CounterAmount, 2),
                RiderOfferAmount = RideRequest.OfferAmount,
                RecommendedAmount = RideRequest.RecommendedAmount,
                IsCounterOffer = true,
                EtaToPickupMinutes = RideRequest.EstimatedMinutes <= 0 ? 5 : RideRequest.EstimatedMinutes,
                Distance = RideRequest.EstimatedDistanceKm <= 0 ? 2.3m : decimal.Round((decimal)RideRequest.EstimatedDistanceKm, 1),
                PickupAddress = RideRequest.StartAddress,
                DestinationAddress = RideRequest.DestinationAddress,
                PickupLocation = RideRequest.StartLocation,
                DestinationLocation = RideRequest.DestinationLocation,
                OfferTime = DateTime.UtcNow,
                Driver = new DriverModel
                {
                    DriverId = Guid.TryParse(session.UserId, out var driverId) ? driverId : Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(session.FullName) ? "Kinetic Anchor" : session.FullName,
                    PhoneNumber = session.PhoneNumber,
                    Rating = 4.8,
                    RidesCompleted = 487,
                    Vehicle = "Toyota Aqua"
                }
            });

            await Shell.Current.DisplayAlert("Counter offer sent", $"Your ${CounterAmount:F2} offer has been sent to the rider.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to submit driver counteroffer");
            await Shell.Current.DisplayAlert("Counter offer failed", "We couldn't send the counteroffer right now.", "OK");
        }
    }
}
