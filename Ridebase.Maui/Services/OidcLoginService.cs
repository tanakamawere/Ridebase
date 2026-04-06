using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Duende.IdentityModel.OidcClient;
using OidcBrowser = Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Configuration;

namespace Ridebase.Services;

/// <summary>
/// Result of an OIDC login or refresh attempt.
/// </summary>
public sealed class OidcLoginResult
{
    public bool IsError { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string? IdToken { get; init; }
    public ClaimsPrincipal? User { get; init; }
}

/// <summary>
/// Authenticates via OAuth2 Authorization Code + PKCE using Duende OidcClient.
/// Login drives Authentik's flow executor headlessly while PKCE secures the token exchange.
/// Refresh uses a direct POST for efficiency (no discovery round-trip).
/// Register as a singleton.
/// </summary>
public class OidcLoginService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://auth.ridebase.tech")
    };

    private readonly string _authority;
    private readonly string _clientId;
    private readonly string _scopes;
    private readonly string _redirectUri;
    private readonly string _tokenEndpoint;

    // Cached client with mutable browser — avoids re-fetching OIDC discovery on each login
    private readonly BrowserProxy _browserProxy = new();
    private OidcClient? _cachedClient;

    public OidcLoginService(IConfiguration configuration)
    {
        _authority = configuration["Auth:Authority"] ?? "https://auth.ridebase.tech/application/o/ridebase/";
        _clientId = configuration["Auth:ClientId"] ?? "ridebase";
        _scopes = configuration["Auth:Scopes"] ?? "openid profile email offline_access";
        _redirectUri = configuration["Auth:RedirectUri"] ?? "ridebase://callback";
        _tokenEndpoint = "/application/o/token/";
    }

    /// <summary>
    /// Login using Authorization Code + PKCE.
    /// Credentials are submitted through Authentik's flow executor API (headless),
    /// while PKCE secures the token exchange.
    /// </summary>
    public async Task<OidcLoginResult> LoginAsync(string username, string password)
    {
        try
        {
            _browserProxy.Inner = new HeadlessAuthentikBrowser(username, password);
            var client = GetOrCreateClient();

            var result = await client.LoginAsync(new LoginRequest());

            if (result.IsError)
            {
                return new OidcLoginResult
                {
                    IsError = true,
                    Error = result.Error,
                    ErrorDescription = result.ErrorDescription
                };
            }

            return new OidcLoginResult
            {
                IsError = false,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                IdToken = result.IdentityToken,
                User = result.User
            };
        }
        catch (Exception ex)
        {
            return new OidcLoginResult
            {
                IsError = true,
                Error = "login_error",
                ErrorDescription = ex.Message
            };
        }
    }

    /// <summary>
    /// Pre-warm TLS to auth server on startup.
    /// </summary>
    public async Task PreWarmAsync()
    {
        try
        {
            // Pre-warm TLS connection to auth server (reused by HeadlessAuthentikBrowser)
            await HeadlessAuthentikBrowser.WarmConnectionAsync();

            // Pre-create OidcClient so OIDC discovery is cached for first login
            GetOrCreateClient();
        }
        catch
        {
            // Best-effort
        }
    }

    /// <summary>
    /// Exchange a refresh token for a new access token.
    /// Uses a direct POST for efficiency (no OIDC discovery round-trip needed).
    /// </summary>
    public async Task<OidcLoginResult> RefreshAsync(string refreshToken)
    {
        try
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["refresh_token"] = refreshToken
            });

            var resp = await Http.PostAsync(_tokenEndpoint, form);
            var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();

            if (!resp.IsSuccessStatusCode || body?.AccessToken is null)
            {
                return new OidcLoginResult
                {
                    IsError = true,
                    Error = body?.Error ?? "refresh_failed",
                    ErrorDescription = body?.ErrorDescription ?? "Token refresh failed."
                };
            }

            return new OidcLoginResult
            {
                IsError = false,
                AccessToken = body.AccessToken,
                RefreshToken = body.RefreshToken,
                IdToken = body.IdToken,
            };
        }
        catch (HttpRequestException ex)
        {
            return new OidcLoginResult
            {
                IsError = true,
                Error = "network_error",
                ErrorDescription = $"Network error: {ex.Message}"
            };
        }
    }

    private OidcClient GetOrCreateClient()
    {
        if (_cachedClient is not null)
            return _cachedClient;

        _cachedClient = new OidcClient(new OidcClientOptions
        {
            Authority = _authority,
            ClientId = _clientId,
            Scope = _scopes,
            RedirectUri = _redirectUri,
            Browser = _browserProxy,
            Policy = new Duende.IdentityModel.OidcClient.Policy
            {
                Discovery = new Duende.IdentityModel.Client.DiscoveryPolicy
                {
                    ValidateEndpoints = false
                }
            }
        });
        return _cachedClient;
    }

    private sealed class BrowserProxy : OidcBrowser.IBrowser
    {
        public OidcBrowser.IBrowser? Inner { get; set; }

        public Task<OidcBrowser.BrowserResult> InvokeAsync(OidcBrowser.BrowserOptions options, CancellationToken ct = default)
        {
            return Inner?.InvokeAsync(options, ct)
                ?? Task.FromResult(new OidcBrowser.BrowserResult
                {
                    ResultType = OidcBrowser.BrowserResultType.UnknownError,
                    Error = "No browser configured"
                });
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
