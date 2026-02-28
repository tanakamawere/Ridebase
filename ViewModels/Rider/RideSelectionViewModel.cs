using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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

    public RideSelectionViewModel(WebSocketClient webSocket, ILogger<RideSelectionViewModel> logger)
    {
        Logger = logger;
        webSocketClient = webSocket;
        DriversList = new ObservableCollection<DriverOfferSelectionModel>();

        webSocketClient.OnMessageReceived += HandleWebSocketMessage;
        webSocketClient.OnConnected += WebSocketClient_OnConnected;
        webSocketClient.OnDisconnected += WebSocketClient_OnDisconnected;

        ConnectWebSocket();
    }

    private async void ConnectWebSocket()
    {
        Logger.LogInformation("Connecting to WebSocket for ride selection");
        IsBusy = true;

        try
        {
            var uri = new Uri("ws://ridebase.app/ws/connect_rider?location=" + JsonSerializer.Serialize(RideRequest.StartLocation));

            Logger.LogInformation("WebSocket URI: {Uri}", uri);
            await webSocketClient.ConnectAsync(uri);
            Logger.LogInformation("WebSocket connected successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error connecting to WebSocket");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void WebSocketClient_OnConnected()
    {
        Logger.LogInformation("WebSocket connection established");
    }

    private void WebSocketClient_OnDisconnected()
    {
        Logger.LogInformation("WebSocket disconnected");
    }

    private void HandleWebSocketMessage(string message)
    {
        Logger.LogInformation("Received WebSocket message: {Message}", message);
        try
        {
            var driver = JsonSerializer.Deserialize<DriverOfferSelectionModel>(message);
            if (driver != null)
            {
                Logger.LogInformation("Driver offer received: DriverId={DriverId}, OfferAmount={OfferAmount}", driver.Driver?.DriverId, driver.OfferAmount);
                // Update the Drivers collection on the UI thread
                App.Current.Dispatcher.Dispatch(() => {
                    DriversList.Add(driver);
                });
            }
            else
            {
                Logger.LogWarning("Failed to deserialize driver offer from message");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling WebSocket message");
        }
    }

    [RelayCommand]
    public async void SelectDriver(DriverOfferSelectionModel driver)
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
            RideId = RideRequest.Id.ToString(),
            DriverId = driver.Driver.DriverId,
            RiderId = RideRequest.RiderId,
            OfferAmount = driver.OfferAmount,
            Status = RideStatus.Offer_Accepted,
            StartLocation = RideRequest.StartLocation,
            DestinationLocation = RideRequest.DestinationLocation
        };

        try
        {
            //Send message to API
            Logger.LogInformation("Sending driver accept request via WebSocket");
            await webSocketClient.SendMessageAsync(driverAcceptRequest);
            Logger.LogInformation("Driver accept request sent successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending driver accept request");
        }
    }
}
