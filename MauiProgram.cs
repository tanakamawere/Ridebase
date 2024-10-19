using Auth0.OidcClient;
using CommunityToolkit.Maui;
using DevExpress.Maui;
using Maui.GoogleMaps.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using Ridebase.Pages.Rider;
using Ridebase.Pages.Rider.Dialogs;
using Ridebase.Services;
using Ridebase.Services.Directions;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;
using Ridebase.Services.RideService;
using Ridebase.ViewModels;
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
            //ViewModels registration
            builder.Services.AddSingleton<MapHomeViewModel>();
            builder.Services.AddSingleton<AppShellViewModel>();

            builder.Services.AddHttpClient<IGeocodeGoogle, GeocodingGoogle>();
            builder.Services.AddHttpClient<IRideService, RideService>();
            builder.Services.AddHttpClient<IDirections, DirectionsService>();
            builder.Services.AddHttpClient<IPlaces, PlacesService>();

            builder.Services.AddSingleton(Connectivity.Current);

#if ANDROID
            builder.Services.AddSingleton<IKeyboardService, Platforms.Android.KeyboardService>();
#elif IOS
            builder.Services.AddSingleton<IKeyboardService, Platforms.iOS.KeyboardService>();
#endif

            //Pages registration
            builder.Services.AddSingleton<MapHomePage>();

            builder.Services.AddSingleton<IGeocodeGoogle, GeocodingGoogle>();
            builder.Services.AddSingleton<IRideService, RideService>();
            builder.Services.AddSingleton<IDirections, DirectionsService>();
            builder.Services.AddTransient<IPlaces, PlacesService>();

            builder.Services.AddSingleton(Mopups.Services.MopupService.Instance);
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
