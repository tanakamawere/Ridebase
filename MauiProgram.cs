using Auth0.OidcClient;
using CommunityToolkit.Maui;
using DevExpress.Maui;
using Maui.GoogleMaps.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using MPowerKit.GoogleMaps;
using Ridebase.Pages.Rider;
using Ridebase.Services;
using Ridebase.Services.Directions;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;
using Ridebase.Services.RestService;
using Ridebase.Services.RideService;
using Ridebase.ViewModels;
using Ridebase.ViewModels.Rider;
using System.Reflection;
using GoogleApi;
using GoogleApi.Extensions;

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
#if ANDROID
                .UseGoogleMaps()
#elif IOS
                .UseGoogleMaps("AIzaSyC9E6Ot4Ui240f88-BGAzUFM-IhPEzT98Y")
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("fasolid.otf", "fasolid");
                    fonts.AddFont("faregular.otf", "faregular");
                    fonts.AddFont("fabrands.otf", "fabrands");
                    fonts.AddFont("rubik.ttf", "rubik");
                });
            builder.Services.AddSingleton(Connectivity.Current);

            //HttpClient Registration
            builder.Services.AddHttpClient<IApiClient, ApiClient>("RidebaseClient", client =>
            {
                var baseAddress = builder.Configuration["RidebaseEndpoint"];
                if (string.IsNullOrEmpty(baseAddress))
                {
                    throw new ArgumentNullException(nameof(baseAddress), "Ridebase Endpoint configuration is missing or empty.");
                }
                client.BaseAddress = new Uri(baseAddress);
            });

#if ANDROID
            builder.Services.AddSingleton<IKeyboardService, Platforms.Android.KeyboardService>();
#elif IOS
            builder.Services.AddSingleton<IKeyboardService, Platforms.iOS.KeyboardService>();
#endif

            //ViewModels registration
            builder.Services.AddSingleton<HomePageViewModel>();
            builder.Services.AddTransient<SearchPageViewModel>();
            builder.Services.AddSingleton<AppShellViewModel>();

            //Rider Pages registration
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddTransient<SearchPage>();
            builder.Services.AddTransient<RideDetailsPage>();
            builder.Services.AddTransient<RideEndedPage>();
            builder.Services.AddTransient<RideHistoryPage>();
            builder.Services.AddScoped<RideProgressPage>();
            builder.Services.AddTransient<ProfilePage>();

            builder.Services.AddScoped<IApiClient, ApiClient>();
            builder.Services.AddScoped<WebSocketClient>();
            builder.Services.AddSingleton<IGeocodeGoogle, GeocodingGoogle>();
            builder.Services.AddSingleton<IRideService, RideService>();
            builder.Services.AddSingleton<IDirections, DirectionsService>();
            builder.Services.AddTransient<IPlaces, PlacesService>();

            builder.Services.AddSingleton(Mopups.Services.MopupService.Instance);
            builder.Services.AddGoogleApiClients();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            //Getting App Settings.json
            var executingAssembly = Assembly.GetExecutingAssembly();
            using var stream = executingAssembly.GetManifestResourceStream("Ridebase.appsettings.json");

            var configuration = new ConfigurationBuilder().AddJsonStream(stream)
                .Build();

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
