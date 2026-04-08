namespace Ridebase.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Launches the system browser for PKCE login via Authentik.
    /// On success, extracts and stores all tokens and custom claims.
    /// </summary>
    Task<AuthLoginResult> LoginAsync();

    /// <summary>
    /// Silently refreshes the access token using the stored refresh token.
    /// Returns true if a new access token was successfully obtained and stored.
    /// </summary>
    Task<bool> TryRefreshAsync();

    /// <summary>
    /// Signs the user out via Authentik's OIDC end-session endpoint,
    /// then clears all locally stored session data.
    /// </summary>
    Task LogoutAsync();
}

/// <summary>
/// Strongly-typed result of a successful OIDC login,
/// including custom Authentik claims decoded from the ID token.
/// </summary>
public class AuthLoginResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    // Core identity
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PictureUrl { get; init; } = string.Empty;

    // Tokens
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }

    // Custom Authentik claims
    public bool IsSubscribed { get; init; }
    public bool EmailVerified { get; init; }
    public List<string> Groups { get; init; } = [];
    public int? AuthentikPk { get; init; }
}
