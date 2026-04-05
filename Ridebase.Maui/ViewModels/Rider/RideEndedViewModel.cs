using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models.Ride;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Rider;

public partial class RideEndedViewModel : BaseViewModel
{
    [ObservableProperty]
    private string driverName = "Driver";

    [ObservableProperty]
    private string destinationText = "Destination";

    [ObservableProperty]
    private string fareText = "$0.00";

    [ObservableProperty]
    private int selectedRating = 5;

    [ObservableProperty]
    private string feedback = string.Empty;

    public RideEndedViewModel(IRideStateStore rideStateStore, IRideApiClient rideApiClient)
    {
        this.rideStateStore = rideStateStore;
        this.rideApiClient = rideApiClient;
        Title = "Rate your driver";

        if (rideStateStore.CurrentRide is not null)
        {
            DriverName = rideStateStore.CurrentRide.DriverName;
            DestinationText = rideStateStore.CurrentRide.DestinationAddress;
            FareText = $"${rideStateStore.CurrentRide.AcceptedAmount:F2}";
        }
    }

    [RelayCommand]
    public void SetRating(string value)
    {
        if (int.TryParse(value, out var parsed))
        {
            SelectedRating = Math.Clamp(parsed, 1, 5);
        }
    }

    [RelayCommand]
    public async Task SubmitRating()
    {
        var ride = rideStateStore.CurrentRide;
        if (ride is null)
        {
            await Shell.Current.GoToAsync("//Home");
            return;
        }

        ride.RiderRating = SelectedRating;
        ride.RiderFeedback = Feedback;
        rideStateStore.SetCurrentRide(ride);

        await rideApiClient.SubmitRating(new RideRatingRequest
        {
            RideId = ride.RideId,
            RiderId = ride.RiderId,
            DriverId = ride.DriverId,
            Rating = SelectedRating,
            Feedback = Feedback
        });

        rideStateStore.SetCurrentRide(null);
        await Shell.Current.GoToAsync("//Home");
    }
}
