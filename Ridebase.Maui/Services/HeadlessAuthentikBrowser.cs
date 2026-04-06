using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityModel.OidcClient.Browser;

namespace Ridebase.Services;

/// <summary>
/// Drives Authentik's flow executor API headlessly (no browser window).
/// Implements IBrowser so OidcClient can use it for the authorize round-trip.
/// Uses a shared SocketsHttpHandler for TLS connection reuse across logins,
/// with a per-login CookieContainer for proper cookie jar semantics (like curl -b/-c).
/// </summary>
public class HeadlessAuthentikBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Shared handler keeps TLS connections alive across logins — avoids TLS handshake per login.
    // Cookies are OFF here; a per-login CookieHandler sits on top for proper jar semantics.
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        UseCookies = false,
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        ConnectTimeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>Pre-warm the TLS connection so first login reuses it.</summary>
    public static async Task WarmConnectionAsync()
    {
        try
        {
            using var http = new HttpClient(SharedHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://auth.ridebase.tech")
            };
            using var req = new HttpRequestMessage(HttpMethod.Head, "/");
            await http.SendAsync(req);
        }
        catch { /* best-effort */ }
    }

    private readonly string? _username;
    private readonly string? _password;

    public HeadlessAuthentikBrowser(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public HeadlessAuthentikBrowser() { }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken ct = default)
    {
        if (_username is not null && _password is not null)
            return await HeadlessLoginAsync(options.StartUrl, ct);

        return new BrowserResult
        {
            ResultType = BrowserResultType.UnknownError,
            Error = "No credentials provided"
        };
    }

    private async Task<BrowserResult> HeadlessLoginAsync(string authorizeUrl, CancellationToken ct)
    {
        // Fresh CookieContainer per login — proper jar semantics (domain, path, HttpOnly, etc.)
        // layered over the shared SocketsHttpHandler that keeps TLS connections alive.
        var cookieContainer = new CookieContainer();
        using var cookieHandler = new CookieHandler(cookieContainer) { InnerHandler = SharedHandler };
        using var http = new HttpClient(cookieHandler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://auth.ridebase.tech")
        };

        try
        {
            // Step 1 — init authentication flow
            Debug.WriteLine("[LOGIN] Step 1 — init auth flow");
            var initResp = await http.GetAsync("/api/v3/flows/executor/ridebase-authentication/", ct);
            var initRaw = await initResp.Content.ReadAsStringAsync(ct);
            Debug.WriteLine($"[LOGIN] init status={initResp.StatusCode}, body={initRaw[..Math.Min(initRaw.Length, 200)]}");
            initResp.EnsureSuccessStatusCode();
            var initBody = JsonSerializer.Deserialize<FlowResponse>(initRaw, JsonOptions);
            Debug.WriteLine($"[LOGIN] init component={initBody?.Component}");

            if (initBody?.Component == "ak-stage-flow-error")
            {
                return Error("Authentication flow error. Please try again.");
            }

            // Extract CSRF token from cookie jar
            var allCookies = cookieContainer.GetCookies(new Uri("https://auth.ridebase.tech"));
            var csrfToken = allCookies["authentik_csrf"]?.Value;
            Debug.WriteLine($"[LOGIN] cookie count={allCookies.Count}, CSRF present={csrfToken is not null}");

            // Step 2 — submit username / email
            var uidPayload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["component"] = "ak-stage-identification",
                ["uid_field"] = _username!
            });
            Debug.WriteLine($"[LOGIN] Step 2 — POST uid: {uidPayload}");
            var uidRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v3/flows/executor/ridebase-authentication/")
            {
                Content = new StringContent(uidPayload, System.Text.Encoding.UTF8, "application/json")
            };
            if (csrfToken is not null)
                uidRequest.Headers.TryAddWithoutValidation("X-authentik-CSRF", csrfToken);
            var uidResp = await http.SendAsync(uidRequest, ct);
            // Authentik may 302 back to the flow executor — follow to get JSON
            var uidRaw = await FollowFlowRedirectAsync(http, uidResp, ct);
            Debug.WriteLine($"[LOGIN] uid status={uidResp.StatusCode}, body={uidRaw[..Math.Min(uidRaw.Length, 200)]}");
            var uidBody = JsonSerializer.Deserialize<FlowResponse>(uidRaw, JsonOptions);
            Debug.WriteLine($"[LOGIN] uid component={uidBody?.Component}");

            if (uidBody?.ResponseErrors is not null)
            {
                return Error(ExtractFirstError(uidBody.ResponseErrors) ?? "User not found.");
            }
            if (uidBody?.Component == "ak-stage-flow-error")
            {
                return Error("Authentication failed at identification.");
            }

            // Step 3 — submit password
            var pwPayload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["component"] = "ak-stage-password",
                ["password"] = _password!
            });
            Debug.WriteLine("[LOGIN] Step 3 — POST password");
            var pwRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v3/flows/executor/ridebase-authentication/")
            {
                Content = new StringContent(pwPayload, System.Text.Encoding.UTF8, "application/json")
            };
            if (csrfToken is not null)
                pwRequest.Headers.TryAddWithoutValidation("X-authentik-CSRF", csrfToken);
            var pwResp = await http.SendAsync(pwRequest, ct);
            var pwRaw = await FollowFlowRedirectAsync(http, pwResp, ct);
            Debug.WriteLine($"[LOGIN] pw status={pwResp.StatusCode}, body={pwRaw[..Math.Min(pwRaw.Length, 200)]}");
            var pwBody = JsonSerializer.Deserialize<FlowResponse>(pwRaw, JsonOptions);
            Debug.WriteLine($"[LOGIN] pw component={pwBody?.Component}");

            if (pwBody?.ResponseErrors is not null)
            {
                var pwError = ExtractFirstError(pwBody.ResponseErrors);
                return Error(pwError ?? "Invalid password.");
            }
            if (pwBody?.Component == "ak-stage-flow-error")
            {
                return Error("Authentication failed. Please check your credentials.");
            }

            // Step 4 — hit the authorize URL (built by OidcClient with PKCE)
            // AllowAutoRedirect is OFF so we can capture the ridebase:// callback
            Debug.WriteLine($"[LOGIN] Step 4 — GET authorize URL");
            var authResp = await http.GetAsync(authorizeUrl, ct);
            Debug.WriteLine($"[LOGIN] authorize status={authResp.StatusCode}");

            // Follow redirects manually until we hit the callback URI
            var location = authResp.Headers.Location?.ToString();
            Debug.WriteLine($"[LOGIN] authorize location={location}");
            const int maxRedirects = 10;
            for (int i = 0; i < maxRedirects && location is not null && !location.StartsWith("ridebase://", StringComparison.OrdinalIgnoreCase); i++)
            {
                var nextUri = location.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? location
                    : new Uri(http.BaseAddress!, location).ToString();

                Debug.WriteLine($"[LOGIN] following redirect {i}: {nextUri}");
                authResp = await http.GetAsync(nextUri, ct);
                location = authResp.Headers.Location?.ToString();
                Debug.WriteLine($"[LOGIN] redirect {i} status={authResp.StatusCode}, location={location}");
            }

            if (location is null || !location.StartsWith("ridebase://", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[LOGIN] FAILED — final location: {location}");
                return Error($"Unexpected redirect: {location ?? "(none)"}");
            }

            Debug.WriteLine($"[LOGIN] SUCCESS — callback: {location[..Math.Min(location.Length, 80)]}...");

            return new BrowserResult
            {
                ResultType = BrowserResultType.Success,
                Response = location
            };
        }
        catch (HttpRequestException ex)
        {
            return Error($"Network error: {ex.Message}");
        }
    }

    /// <summary>
    /// If the response is a 302, follow up to 3 redirects with GET to reach the JSON stage response.
    /// Cookies are managed automatically by the CookieHandler.
    /// </summary>
    private static async Task<string> FollowFlowRedirectAsync(HttpClient http, HttpResponseMessage resp, CancellationToken ct)
    {
        for (int i = 0; i < 3 && resp.StatusCode == HttpStatusCode.Found; i++)
        {
            var loc = resp.Headers.Location;
            if (loc is null) break;
            var nextUri = loc.IsAbsoluteUri ? loc.ToString() : new Uri(http.BaseAddress!, loc).ToString();
            Debug.WriteLine($"[LOGIN] following flow redirect {i}: {nextUri}");
            resp = await http.GetAsync(nextUri, ct);
        }
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// DelegatingHandler that injects a per-login CookieContainer on top of a shared
    /// SocketsHttpHandler (which provides TLS connection pooling). Like curl -b/-c cookiefile.
    /// </summary>
    private sealed class CookieHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies;

        public CookieHandler(CookieContainer cookies) => _cookies = cookies;

        // Prevent disposing the shared InnerHandler when this per-login handler is disposed
        protected override void Dispose(bool disposing) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Inject stored cookies into the request
            var uri = request.RequestUri!;
            var cookieHeader = _cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            var response = await base.SendAsync(request, ct);

            // Harvest Set-Cookie headers into the jar
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var raw in setCookies)
                    _cookies.SetCookies(uri, raw);
            }

            return response;
        }
    }

    private static BrowserResult Error(string message) => new()
    {
        ResultType = BrowserResultType.UnknownError,
        Error = message
    };

    private static string? ExtractFirstError(Dictionary<string, List<FlowErrorDetail>>? errors)
    {
        if (errors is null) return null;
        foreach (var kvp in errors)
        {
            foreach (var detail in kvp.Value)
            {
                if (!string.IsNullOrWhiteSpace(detail.String))
                    return detail.String;
            }
        }
        return null;
    }

    // Minimal models for Authentik flow executor responses
    private sealed class FlowResponse
    {
        [JsonPropertyName("component")]
        public string? Component { get; set; }
        [JsonPropertyName("to")]
        public string? To { get; set; }
        [JsonPropertyName("response_errors")]
        public Dictionary<string, List<FlowErrorDetail>>? ResponseErrors { get; set; }
        [JsonPropertyName("fields")]
        public List<FlowField>? Fields { get; set; }
    }

    private sealed class FlowErrorDetail
    {
        [JsonPropertyName("string")]
        public string? String { get; set; }
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    private sealed class FlowField
    {
        [JsonPropertyName("field_key")]
        public string? FieldKey { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }
}
