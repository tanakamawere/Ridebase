using Auth0.OidcClient;
using CommunityToolkit.Maui;
using DevExpress.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using MPowerKit.GoogleMaps;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.RestService;
using Ridebase.ViewModels;
using Ridebase.ViewModels.Rider;
using System.Reflection;
using GoogleApi;
using GoogleApi.Extensions;
using Ridebase.ViewModels.Driver;
using Ridebase.Pages.Driver;
using Ridebase.Services.Interfaces;
using Ridebase.Services.ApiClients;

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

            //Rider Pages registration
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddTransient<SearchPage>();
            builder.Services.AddTransient<RideDetailsPage>();
            builder.Services.AddTransient<RideEndedPage>();
            builder.Services.AddTransient<RideHistoryPage>();
            builder.Services.AddScoped<RideProgressPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<RideSelectionPage>();

            //DRIVER'S SIDE
            builder.Services.AddSingleton<DriverDashboardViewModel>();
            builder.Services.AddTransient<DriverProfileViewModel>();
            builder.Services.AddScoped<DriverRideProgressViewModel>();
            builder.Services.AddSingleton<DriverShellViewModel>();
            //pages
            builder.Services.AddSingleton<DriverDashboardPage>();
            builder.Services.AddTransient<DriverProfilePage>();
            builder.Services.AddScoped<DriverRideProgressPage>();

            builder.Services.AddTransient<AuthHeaderHandler>(); // Inject Custom Handler
            builder.Services.AddScoped<WebSocketClient>();
            builder.Services.AddTransient<IAuthenticationClient, AuthenticationApiClient>();
            builder.Services.AddSingleton<IRideApiClient, RideApiClient>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton(Mopups.Services.MopupService.Instance);
            builder.Services.AddGoogleApiClients();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(new Auth0Client(new()
            {
                Domain = configuration["Auth0:Domain"],
                ClientId = configuration["Auth0:ClientId"],
                Scope = configuration["Auth0:Scopes"],
                RedirectUri = configuration["Auth0:RedirectUri"],
            }));

            return builder.Build();
        }
    }
}
