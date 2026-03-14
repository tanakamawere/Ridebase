using CommunityToolkit.Maui;
using DevExpress.Maui;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using GoogleApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using MPowerKit.GoogleMaps;
using Ridebase.Pages.Driver;
using Ridebase.Pages.Onboarding;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.ApiClients;
using Ridebase.Services.Interfaces;
using Ridebase.Services.RestService;
using Ridebase.ViewModels;
using Ridebase.ViewModels.Driver;
using Ridebase.ViewModels.Onboarding;
using Ridebase.ViewModels.Rider;
using System.Reflection;

namespace Ridebase
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseDevExpress()
                .UseDevExpressControls()
                .UseMPowerKitGoogleMaps(
#if IOS
                    "AIzaSyC9E6Ot4Ui240f88-BGAzUFM-IhPEzT98Y"
#endif
                )
                .ConfigureMopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("fasolid.otf", "fasolid");
                    fonts.AddFont("faregular.otf", "faregular");
                    fonts.AddFont("fabrands.otf", "fabrands");
                    fonts.AddFont("rubik.ttf", "rubik");
                });


            //Getting App Settings.json
            var executingAssembly = Assembly.GetExecutingAssembly();
            using var stream = executingAssembly.GetManifestResourceStream("Ridebase.appsettings.json");

            var configuration = new ConfigurationBuilder().AddJsonStream(stream)
                .Build();

            var useMockServices = configuration.GetValue<bool>("UseMockServices");

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddSingleton(Connectivity.Current);

            //HttpClient Registration
            builder.Services.AddHttpClient<IApiClient, ApiClient>("RidebaseClient", client =>
            {
                var baseAddress = configuration["RidebaseEndpoint"];
                if (string.IsNullOrEmpty(baseAddress))
                {
                    throw new ArgumentNullException(nameof(baseAddress), "Ridebase Endpoint configuration is missing or empty.");
                }
                client.BaseAddress = new Uri(baseAddress);
            })
                .AddHttpMessageHandler<AuthHeaderHandler>();

#if ANDROID
            builder.Services.AddSingleton<IKeyboardService, Platforms.Android.KeyboardService>();
#elif IOS
            builder.Services.AddSingleton<IKeyboardService, Platforms.iOS.KeyboardService>();
#endif

            //ViewModels registration
            builder.Services.AddSingleton<HomePageViewModel>();
            builder.Services.AddTransient<SearchPageViewModel>();
            builder.Services.AddTransient<RideDetailsViewModel>();
            builder.Services.AddSingleton<AppShellViewModel>();
            builder.Services.AddSingleton<RideSelectionViewModel>();
            builder.Services.AddTransient<RideProgressViewModel>();

            //Onboarding ViewModels
            builder.Services.AddTransient<OnboardingProfileViewModel>();
            builder.Services.AddTransient<OnboardingRoleViewModel>();
            builder.Services.AddTransient<OnboardingDriverViewModel>();

            //Rider Pages registration
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddTransient<SearchPage>();
            builder.Services.AddTransient<RideDetailsPage>();
            builder.Services.AddTransient<RideEndedPage>();
            builder.Services.AddTransient<RideHistoryPage>();
            builder.Services.AddTransient<RideProgressPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<RideSelectionPage>();

            //Onboarding Pages
            builder.Services.AddTransient<OnboardingProfilePage>();
            builder.Services.AddTransient<OnboardingRolePage>();
            builder.Services.AddTransient<OnboardingDriverPage>();

            //DRIVER'S SIDE
            builder.Services.AddSingleton<DriverDashboardViewModel>();
            builder.Services.AddSingleton<DriverProfileViewModel>();
            builder.Services.AddTransient<DriverRideProgressViewModel>();
            builder.Services.AddSingleton<DriverShellViewModel>();
            builder.Services.AddSingleton<DriverStatsViewModel>();
            //pages
            builder.Services.AddSingleton<DriverDashboardPage>();
            builder.Services.AddSingleton<DriverProfilePage>();
            builder.Services.AddTransient<DriverRideProgressPage>();
            builder.Services.AddSingleton<DriverStatsPage>();

            builder.Services.AddTransient<AuthHeaderHandler>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<IUserSessionService, UserSessionService>();
            builder.Services.AddSingleton<IUserBootstrapService, MockUserBootstrapService>();
            builder.Services.AddSingleton<IRideStateStore, RideStateStore>();

            var realtimeTransport = configuration.GetValue<string>("RealtimeTransport") ?? "WebSocket";


            // Switch between raw WebSocket and SignalR hub via appsettings.json
            // "RealtimeTransport": "WebSocket"  → WebSocketRideRealtimeService
            // "RealtimeTransport": "SignalR"    → SignalRRideRealtimeService
            if (realtimeTransport.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
                builder.Services.AddSingleton<IRideRealtimeService, SignalRRideRealtimeService>();
            else
                builder.Services.AddSingleton<IRideRealtimeService, WebSocketRideRealtimeService>();

            builder.Services.AddSingleton<IRideApiClient, RideApiClient>();
            builder.Services.AddTransient<IOnboardingApiClient, OnboardingApiClient>();
            builder.Services.AddSingleton<IDriverApiClient, DriverApiClient>();

            builder.Services.AddSingleton(Mopups.Services.MopupService.Instance);
            builder.Services.AddGoogleApiClients();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(new OidcClient(new()
            {
                Authority = "https://auth.dev.uzuri.co.uk/application/o/my-csharp-app/",
                ClientId = configuration["Auth:ClientId"],
                Scope = configuration["Auth:Scopes"],
                RedirectUri = configuration["Auth:RedirectUri"],
                Browser = new MauiAuthenticationBrowser(),

                Policy = new Policy
                {
                    Discovery = new DiscoveryPolicy
                    {
                        AdditionalEndpointBaseAddresses =
                        {
                            "https://auth.dev.uzuri.co.uk/application/o/"
                        }
                    }
                }
            }));

            return builder.Build();
        }
    }
}
