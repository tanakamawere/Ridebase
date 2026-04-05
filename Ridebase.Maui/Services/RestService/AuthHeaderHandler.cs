using Ridebase.Services.Interfaces;
using System.Net.Http.Headers;

namespace Ridebase.Services.RestService;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IStorageService _storageService;
    public AuthHeaderHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _storageService.GetAuthTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}