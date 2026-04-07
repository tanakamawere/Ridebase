using Ridebase.Services.Interfaces;
using System.Net.Http.Headers;

namespace Ridebase.Services.RestService;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IStorageService _storageService;
    private readonly IAuthService _authService;

    public AuthHeaderHandler(IStorageService storageService, IAuthService authService)
    {
        _storageService = storageService;
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _storageService.GetAuthTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // On 401 — silently attempt token refresh and retry the request once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await _authService.TryRefreshAsync();
            if (refreshed)
            {
                var newToken = await _storageService.GetAuthTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    // Clone the request — HttpRequestMessage cannot be sent twice
                    var retryRequest = await CloneRequestAsync(request);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await base.SendAsync(retryRequest, cancellationToken);
                }
            }
            else
            {
                // Refresh token is also expired/invalid — log the user out
                await _authService.LogoutAsync();
            }
        }

        return response;
    }

    /// <summary>
    /// Creates a new HttpRequestMessage that is a copy of the original,
    /// because HttpRequestMessage instances cannot be resent after disposal.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}