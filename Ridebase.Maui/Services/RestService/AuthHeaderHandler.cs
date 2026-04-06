using Ridebase.Services.Interfaces;
using System.Net;
using System.Net.Http.Headers;

namespace Ridebase.Services.RestService;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IStorageService _storageService;
    private readonly OidcLoginService _oidcLoginService;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public AuthHeaderHandler(IStorageService storageService, OidcLoginService oidcLoginService)
    {
        _storageService = storageService;
        _oidcLoginService = oidcLoginService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _storageService.GetAuthTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Auto-refresh on 401
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await RefreshLock.WaitAsync(cancellationToken);
                try
                {
                    // Check if another thread already refreshed
                    var currentToken = await _storageService.GetAuthTokenAsync();
                    if (currentToken == token)
                    {
                        var result = await _oidcLoginService.RefreshAsync(refreshToken);
                        if (!result.IsError)
                        {
                            await _storageService.SetAuthTokenAsync(result.AccessToken);
                            if (!string.IsNullOrEmpty(result.RefreshToken))
                                await _storageService.SetRefreshTokenAsync(result.RefreshToken);
                            token = result.AccessToken;
                        }
                    }
                    else
                    {
                        token = currentToken;
                    }
                }
                finally
                {
                    RefreshLock.Release();
                }

                // Retry the request with new token
                using var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}