using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Ridebase.Models.Ride;
using Ridebase.Services.ApiClients;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text.Json;

namespace Ridebase.ViewModels.Rider;

[QueryProperty(nameof(RideRequest), "rideRequest")]
public partial class RideSelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<DriverOfferSelectionModel> driversList;
    private readonly WebSocketClient webSocketClient;
    [ObservableProperty]
    private RideRequestModel rideRequest;

    public RideSelectionViewModel(WebSocketClient webSocket)
    {
        webSocketClient = webSocket;
        DriversList = new ObservableCollection<DriverOfferSelectionModel>();

        webSocketClient.OnMessageReceived += HandleWebSocketMessage;
        webSocketClient.OnConnected += WebSocketClient_OnConnected;
        webSocketClient.OnDisconnected += WebSocketClient_OnDisconnected;

        ConnectWebSocket();
    }

    private async void ConnectWebSocket()
    {
        IsBusy = true;

        try
        {
            var uri = new Uri("ws://ridebase.app/ws/connect_rider?location=" + JsonSerializer.Serialize(RideRequest.StartLocation));

            await webSocketClient.ConnectAsync(uri);
        }
        catch (Exception ex)
        {
            // Log error
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void WebSocketClient_OnConnected()
    {
        // Handle WebSocket connected event if needed
    }

    private void WebSocketClient_OnDisconnected()
    {
        // Handle WebSocket disconnected event if needed
    }

    private void HandleWebSocketMessage(string message)
    {
        var driver = JsonSerializer.Deserialize<DriverOfferSelectionModel>(message);
        if (driver != null)
        {
            // Update the Drivers collection on the UI thread
            App.Current.Dispatcher.Dispatch(() => {
                DriversList.Add(driver);
            });
        }
    }

    [RelayCommand]
    public async void SelectDriver(DriverOfferSelectionModel driver)
    {
        if (driver == null) return;

        //Create Driver Accept Request Object
        var driverAcceptRequest = new RideAcceptRequest
        {
            RideId = RideRequest.Id.ToString(),
            DriverId = driver.Driver.DriverId,
            RiderId = RideRequest.RiderId,
            OfferAmount = driver.OfferAmount,
            Status = RideStatus.Offer_Accepted,
            StartLocation = RideRequest.StartLocation,
            DestinationLocation = RideRequest.DestinationLocation
        };

        //Send message to API
        await webSocketClient.SendMessageAsync(driverAcceptRequest);
    }
}
