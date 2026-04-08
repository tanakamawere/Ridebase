using Android.App;
using Android.Content.PM;

namespace Ridebase;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
// Login callback: handles ridebase://callback?code=...
[IntentFilter([Android.Content.Intent.ActionView],
              Categories = [
                Android.Content.Intent.CategoryDefault,
                Android.Content.Intent.CategoryBrowsable
              ],
              DataScheme = "ridebase",
              DataHost = "callback")]
// Logout callback: handles ridebase://logout-callback
[IntentFilter([Android.Content.Intent.ActionView],
              Categories = [
                Android.Content.Intent.CategoryDefault,
                Android.Content.Intent.CategoryBrowsable
              ],
              DataScheme = "ridebase",
              DataHost = "logout-callback")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}