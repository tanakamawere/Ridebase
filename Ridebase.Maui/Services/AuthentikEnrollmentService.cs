using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ridebase.Services;

/// <summary>
/// Result of an enrollment (sign-up) attempt against Authentik.
/// </summary>
public sealed class EnrollmentResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static EnrollmentResult Ok() => new() { Success = true };
    public static EnrollmentResult Fail(string message) => new() { ErrorMessage = message };
}

/// <summary>
/// Drives Authentik's enrollment flow executor headlessly.
/// </summary>
public class AuthentikEnrollmentService
{
    private const string BaseUrl = "https://auth.ridebase.tech";
    private const string EnrollmentPath = "/api/v3/flows/executor/ridebase-enrollment/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Register a new user account via Authentik's enrollment flow.
    /// </summary>
    public async Task<EnrollmentResult> SignUpAsync(string email, string username, string password, CancellationToken ct = default)
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            AllowAutoRedirect = true,
            CookieContainer = cookieContainer
        };
        var http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        try
        {
            // Step 1 — init enrollment flow (expects ak-stage-prompt)
            var initResp = await http.GetAsync($"{EnrollmentPath}?query=", ct);
            initResp.EnsureSuccessStatusCode();

            var initRaw = await initResp.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[ENROLL] GET response: {initRaw}");
            var initBody = JsonSerializer.Deserialize<FlowResponse>(initRaw, JsonOptions);

            // Log cookies for debugging
            var cookies = cookieContainer.GetCookies(new Uri(BaseUrl));
            Console.WriteLine($"[ENROLL] Cookies after GET: {cookies.Count}");
            foreach (Cookie c in cookies)
                Console.WriteLine($"[ENROLL]   cookie: {c.Name}={c.Value[..Math.Min(c.Value.Length, 20)]}...");

            if (initBody?.Component == "ak-stage-flow-error")
                return EnrollmentResult.Fail("Enrollment flow error. Please try again later.");

            if (initBody?.Component != "ak-stage-prompt")
                return EnrollmentResult.Fail($"Unexpected flow stage: {initBody?.Component}");

            // Step 2 — submit all registration fields as JSON
            var signupPayload = new Dictionary<string, string>
            {
                ["component"] = "ak-stage-prompt",
                ["email"] = email,
                ["username"] = username,
                ["password"] = password,
                ["password_repeat"] = password
            };

            // Include any extra field_keys Authentik expects
            if (initBody.Fields is { Count: > 0 })
            {
                foreach (var field in initBody.Fields)
                {
                    if (field.FieldKey is not null && !signupPayload.ContainsKey(field.FieldKey))
                    {
                        if (field.Type is "email") signupPayload[field.FieldKey] = email;
                        else if (field.Type is "username") signupPayload[field.FieldKey] = username;
                        else if (field.Type is "password") signupPayload[field.FieldKey] = password;
                    }
                }
            }

            var postJson = JsonSerializer.Serialize(signupPayload);
            Console.WriteLine($"[ENROLL] POST body: {postJson}");

            // Build request manually to ensure cookies and CSRF header are set
            var request = new HttpRequestMessage(HttpMethod.Post, EnrollmentPath)
            {
                Content = new StringContent(postJson, System.Text.Encoding.UTF8, "application/json")
            };

            // Authentik (Django) requires CSRF token on session-authenticated POSTs
            var csrfCookie = cookies["authentik_csrf"] ?? cookies["csrftoken"];
            if (csrfCookie is not null)
            {
                request.Headers.Add("X-authentik-CSRF", csrfCookie.Value);
                Console.WriteLine($"[ENROLL] Sending CSRF header: {csrfCookie.Value[..Math.Min(csrfCookie.Value.Length, 20)]}...");
            }
            else
            {
                Console.WriteLine("[ENROLL] WARNING: No CSRF cookie found!");
            }

            // Log cookies being sent on POST
            var postCookies = cookieContainer.GetCookies(new Uri(BaseUrl));
            Console.WriteLine($"[ENROLL] Cookies on POST: {postCookies.Count}");
            foreach (Cookie c in postCookies)
                Console.WriteLine($"[ENROLL]   cookie: {c.Name}");

            var signupResp = await http.SendAsync(request, ct);
            signupResp.EnsureSuccessStatusCode();

            var postRaw = await signupResp.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[ENROLL] POST response: {postRaw}");

            var body = JsonSerializer.Deserialize<FlowResponse>(postRaw, JsonOptions);

            // Step 3 — walk through subsequent stages until completion
            const int maxStages = 5;
            for (int i = 0; i < maxStages; i++)
            {
                Console.WriteLine($"[ENROLL] Stage {i}: component={body?.Component}");

                // Check for validation errors (e.g. duplicate username/email)
                if (body?.ResponseErrors is not null)
                {
                    var error = ExtractFirstError(body.ResponseErrors);
                    http.Dispose();
                    return EnrollmentResult.Fail(error ?? "Sign-up failed. Please check your details.");
                }

                switch (body?.Component)
                {
                    case "xak-flow-redirect":
                        Console.WriteLine($"[ENROLL] Redirect to: {body.To}");
                        http.Dispose();
                        return EnrollmentResult.Ok();

                    case "ak-stage-autosubmit":
                        // Auto-advancing stage — POST empty to continue
                        Console.WriteLine("[ENROLL] Auto-submit stage, advancing...");
                        var autoReq = new HttpRequestMessage(HttpMethod.Post, EnrollmentPath)
                        {
                            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                        };
                        var csrf = cookieContainer.GetCookies(new Uri(BaseUrl))["authentik_csrf"]
                                ?? cookieContainer.GetCookies(new Uri(BaseUrl))["csrftoken"];
                        if (csrf is not null)
                            autoReq.Headers.Add("X-authentik-CSRF", csrf.Value);

                        var nextResp = await http.SendAsync(autoReq, ct);
                        nextResp.EnsureSuccessStatusCode();
                        var nextRaw = await nextResp.Content.ReadAsStringAsync(ct);
                        Console.WriteLine($"[ENROLL] Auto-submit response: {nextRaw}");
                        body = JsonSerializer.Deserialize<FlowResponse>(nextRaw, JsonOptions);
                        continue;

                    case "ak-stage-prompt":
                        // Prompt re-served unexpectedly — treat as error
                        http.Dispose();
                        return EnrollmentResult.Fail("Enrollment prompt was not accepted. Please try again.");

                    default:
                        http.Dispose();
                        return EnrollmentResult.Fail($"Unexpected stage: {body?.Component}");
                }
            }

            http.Dispose();
            return EnrollmentResult.Fail("Enrollment flow did not complete.");
        }
        catch (HttpRequestException ex)
        {
            http.Dispose();
            return EnrollmentResult.Fail($"Network error: {ex.Message}");
        }
    }

    private static string? ExtractFirstError(Dictionary<string, List<FlowErrorDetail>>? errors)
    {
        if (errors is null) return null;
        foreach (var kvp in errors)
        {
            foreach (var detail in kvp.Value)
            {
                if (!string.IsNullOrWhiteSpace(detail.String))
                    return $"{kvp.Key}: {detail.String}";
            }
        }
        return null;
    }

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
