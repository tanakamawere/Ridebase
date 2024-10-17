using Auth0.OidcClient;
using CommunityToolkit.Maui;
using DevExpress.Maui;
using GoogleApi.Extensions;
using Maui.GoogleMaps.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using Ridebase.Pages.Rider;
using Ridebase.Pages.Rider.Dialogs;
using Ridebase.Services.Geocoding;
using Ridebase.Services.Places;
using Ridebase.Services.RideService;
using Ridebase.ViewModels;
using System.Reflection;
using The49.Maui.BottomSheet;
using UraniumUI;

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
                .UseBottomSheet()
                .UseDevExpress()
                .UseUraniumUI()
                .ConfigureMopups()
                .UseUraniumUIMaterial()
#if ANDROID
                .UseGoogleMaps()
#elif IOS
                .UseGoogleMaps("AIzaSyC9E6Ot4Ui240f88-BGAzUFM-IhPEzT98Y")
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFontAwesomeIconFonts();
                });
            //ViewModels registration
            builder.Services.AddSingleton<MapHomeViewModel>();
            builder.Services.AddSingleton<AppShellViewModel>();

            //Pages registration
            builder.Services.AddSingleton<MapHomePage>();

            builder.Services.AddTransient<IGeocodeGoogle, GeocodingGoogle>();
            builder.Services.AddSingleton<IRideService, RideService>();
            builder.Services.AddSingleton<IPlaces, PlacesService>();
            //Google APIs injection
            builder.Services.AddGoogleApiClients();

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
