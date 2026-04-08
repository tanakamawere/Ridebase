using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.Browser;

namespace Ridebase.Services;

public class MauiAuthenticationBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var authOptions = new WebAuthenticatorOptions
            {
                Url = new Uri(options.StartUrl),
                CallbackUrl = new Uri(options.EndUrl),
                PrefersEphemeralWebBrowserSession = true
            };

            var result = await WebAuthenticator.Default.AuthenticateAsync(authOptions);

            // Reconstruct the full callback URL from the properties returned by the OS
            // IMPORTANT: use options.EndUrl (ridebase://callback) not a hard-coded string
            var url = new RequestUrl(options.EndUrl)
                .Create(new Parameters(result.Properties));

            return new BrowserResult
            {
                Response = url,
                ResultType = BrowserResultType.Success,
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult { ResultType = BrowserResultType.UserCancel };
        }
        catch (Exception ex)
        {
            // Logging the technical details to help diagnose redirection failures
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message
            };
        }
    }
}
