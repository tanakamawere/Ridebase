using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Logging;
using Ridebase.Services.Interfaces;

namespace Ridebase.Services;

/// <summary>
/// Handles all OAuth2/OIDC interactions with Authentik.
/// Wraps Duende.IdentityModel.OidcClient to provide:
///   - PKCE login (including post-registration auto sign-in)
///   - Silent token refresh using the stored refresh token
///   - OIDC end-session logout
/// </summary>
public class AuthService : IAuthService
{
    private readonly OidcClient _oidcClient;
    private readonly IUserSessionService _sessionService;
    private readonly ILogger<AuthService> _logger;

    // Semaphore prevents concurrent refresh races (e.g. multiple 401s in flight)
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(
        OidcClient oidcClient,
        IUserSessionService sessionService,
        ILogger<AuthService> logger)
    {
        _oidcClient = oidcClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthLoginResult> LoginAsync()
    {
        try
        {
            var loginResult = await _oidcClient.LoginAsync(new LoginRequest());

            if (loginResult.IsError)
            {
                _logger.LogWarning("OIDC login failed: {Error} — {Description}",
                    loginResult.Error, loginResult.ErrorDescription);

                return new AuthLoginResult
                {
                    Success = false,
                    ErrorMessage = loginResult.ErrorDescription ?? loginResult.Error
                };
            }

            var claims = loginResult.User.Claims.ToList();

            // Core identity claims
            var userId = claims.FirstOrDefault(c => c.Type == "sub")?.Value
                         ?? Guid.NewGuid().ToString("N");
            var email = claims.FirstOrDefault(c => c.Type == "email")?.Value
                        ?? claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? string.Empty;
            var displayName = loginResult.User.Identity?.Name
                              ?? claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                              ?? "Ridebase User";
            var pictureUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? string.Empty;

            // Custom Authentik claims injected via property mappings
            var isSubscribed = ParseBool(claims.FirstOrDefault(c => c.Type == "is_subscribed")?.Value);
            var emailVerified = ParseBool(claims.FirstOrDefault(c => c.Type == "email_verified")?.Value);
            var groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
            var authentikPk = int.TryParse(claims.FirstOrDefault(c => c.Type == "authentik_pk")?.Value, out var pk)
                ? pk : (int?)null;

            // Persist tokens and identity to SecureStorage
            await _sessionService.SetAuthSessionAsync(
                userId,
                loginResult.AccessToken,
                loginResult.RefreshToken,
                loginResult.IdentityToken,
                displayName,
                email,
                pictureUrl);

            _logger.LogInformation(
                "Login successful. User={UserId}, Groups=[{Groups}], Subscribed={Sub}",
                userId, string.Join(",", groups), isSubscribed);

            return new AuthLoginResult
            {
                Success = true,
                UserId = userId,
                DisplayName = displayName,
                Email = email,
                PictureUrl = pictureUrl,
                AccessToken = loginResult.AccessToken,
                RefreshToken = loginResult.RefreshToken,
                IsSubscribed = isSubscribed,
                EmailVerified = emailVerified,
                Groups = groups,
                AuthentikPk = authentikPk
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during OIDC login");
            return new AuthLoginResult
            {
                Success = false,
                ErrorMessage = "Unable to complete authentication. Please try again."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryRefreshAsync()
    {
        // Only one refresh at a time — prevents token storms from concurrent 401s
        if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(10)))
        {
            _logger.LogWarning("Token refresh lock timeout — another refresh is in progress");
            return false;
        }

        try
        {
            var refreshToken = await SecureStorage.GetAsync("refresh_token");
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("No refresh token in SecureStorage — user must log in again");
                return false;
            }

            var refreshResult = await _oidcClient.RefreshTokenAsync(refreshToken);

            if (refreshResult.IsError)
            {
                _logger.LogWarning("Token refresh failed: {Error}", refreshResult.Error);
                return false;
            }

            // Authentik rotates the refresh token when the rotation threshold is passed (7 days).
            // Always store whatever token came back — it may be the same or a new one.
            var newRefreshToken = refreshResult.RefreshToken ?? refreshToken;
            var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
            var displayName = await SecureStorage.GetAsync("user_display_name") ?? string.Empty;
            var email = await SecureStorage.GetAsync("user_email") ?? string.Empty;
            var imageUrl = await SecureStorage.GetAsync("user_image_url") ?? string.Empty;

            await _sessionService.SetAuthSessionAsync(
                userId,
                refreshResult.AccessToken,
                newRefreshToken,
                refreshResult.IdentityToken,
                displayName,
                email,
                imageUrl);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token refresh");
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync()
    {
        try
        {
            // 1. Fetch tokens before we clear them from storage
            var accessToken = await SecureStorage.GetAsync("auth_token");
            var refreshToken = await SecureStorage.GetAsync("refresh_token");
            var idToken = await SecureStorage.GetAsync("id_token");

            // 2. Clear local session IMMEDIATELY (Instant UI Feedback)
            // This ensures the app returns to the login screen instantly,
            // matching the user's "this is fine, just hit the endpoints" preference.
            await _sessionService.ClearSessionAsync();

            // 3. Resolve the EndSessionEndpoint and build the URL manually
            using var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(_oidcClient.Options.Authority);
            
            string logoutUrl;
            var redirectUriEnc = System.Net.WebUtility.UrlEncode("ridebase://callback");
            
            if (!disco.IsError && !string.IsNullOrWhiteSpace(disco.EndSessionEndpoint))
            {
                logoutUrl = $"{disco.EndSessionEndpoint}?id_token_hint={idToken}&post_logout_redirect_uri={redirectUriEnc}";
            }
            else
            {
                var baseUrl = _oidcClient.Options.Authority.TrimEnd('/');
                logoutUrl = $"{baseUrl}/end-session/?id_token_hint={idToken}&post_logout_redirect_uri={redirectUriEnc}";
            }

            // 4. Hit the Browser Logout FIRST with a 10s timeout
            // This gives Authentik plenty of room to process the redirect, 
            // but ensures the server-side revocation always follows up.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try 
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _oidcClient.Options.Browser.InvokeAsync(new BrowserOptions(logoutUrl, "ridebase://callback")
                    {
                        DisplayMode = DisplayMode.Visible
                    }, cts.Token);
                });
            }
            catch (Exception)
            {
                // Proceed immediately if browser fails, cancels, or times out
            }

            // 5. Hit the Revocation Endpoint LAST
            // We await this to ensure the tokens are dead before the method returns.
            await RevokeTokensInternalAsync(accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OIDC logout sequence encountered an error");
        }
    }

    private async Task RevokeTokensInternalAsync(string? accessToken, string? refreshToken)
    {
        try
        {
            var options = _oidcClient.Options;
            using var client = new HttpClient();
            
            // Standard way to fetch discovery doc in IdentityModel
            var disco = await client.GetDiscoveryDocumentAsync(options.Authority);
            
            if (disco.IsError || string.IsNullOrWhiteSpace(disco.RevocationEndpoint))
            {
                _logger.LogWarning("Could not resolve revocation endpoint from discovery: {Error}", disco.Error);
                return;
            }

            var clientId = _oidcClient.Options.ClientId;
            
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                await client.RevokeTokenAsync(new TokenRevocationRequest
                {
                    Address = disco.RevocationEndpoint,
                    ClientId = options.ClientId,
                    Token = accessToken,
                    TokenTypeHint = "access_token"
                });
            }

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await client.RevokeTokenAsync(new TokenRevocationRequest
                {
                    Address = disco.RevocationEndpoint,
                    ClientId = options.ClientId,
                    Token = refreshToken,
                    TokenTypeHint = "refresh_token"
                });
            }

            _logger.LogInformation("Background token revocation completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token revocation failed silently (non-critical for overall logout)");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var result) && result;
}
