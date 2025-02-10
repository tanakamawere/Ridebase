using Ridebase.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ridebase.Services.ApiClients;

public class WebSocketClient
{
    private ClientWebSocket _clientWebSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private Uri _serverUri;

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    private readonly IStorageService storageService;

    private bool _isReconnecting;

    public WebSocketClient(IStorageService _storage)
    {
        storageService = _storage;
    }

    public async Task ConnectAsync(Uri uri)
    {
        _serverUri = uri; // Save the URI for reconnection
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            _clientWebSocket = new ClientWebSocket();

            //Set the WebSocket options (Auth token)
            _clientWebSocket.Options.SetRequestHeader("Authorization", "Bearer " + await storageService.GetAuthTokenAsync());

            await _clientWebSocket.ConnectAsync(uri, CancellationToken.None);

            OnConnected?.Invoke(); // Notify that the connection is established

            _isReconnecting = false; // Reset the reconnect flag
            _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token)); // Start receiving messages
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
            await AttemptReconnect(); // Attempt to reconnect
        }
    }

    public async Task DisconnectAsync()
    {
        _cancellationTokenSource?.Cancel();

        if (_clientWebSocket?.State == WebSocketState.Open)
        {
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
        }

        _clientWebSocket?.Dispose();
        OnDisconnected?.Invoke(); // Notify that the connection is closed
    }


    public async Task SendMessageAsync<T>(T message)
    {
        if (_clientWebSocket?.State == WebSocketState.Open)
        {
            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            var segment = new ArraySegment<byte>(messageBytes);
            await _clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken token)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (_clientWebSocket?.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("WebSocket connection closed by server.");
                    await AttemptReconnect();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessageReceived?.Invoke(message); // Notify about the received message
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
            await AttemptReconnect();
        }
    }

    private async Task AttemptReconnect()
    {
        if (_isReconnecting || string.IsNullOrEmpty(_serverUri.ToString()))
            return;

        _isReconnecting = true;

        Console.WriteLine("Attempting to reconnect...");
        while (_isReconnecting)
        {
            try
            {
                await Task.Delay(2000); // Wait for 5 seconds before retrying
                await ConnectAsync(_serverUri); // Retry connection
                _isReconnecting = false; // Stop reconnection attempts once successful
            }
            catch
            {
                Console.WriteLine("Reconnect attempt failed. Retrying...");
            }
        }
    }
}
