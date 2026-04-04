using Ridebase.Pages.Driver;
using Ridebase.Pages.Onboarding;
using Ridebase.Pages.Rider;
using Ridebase.ViewModels;

namespace Ridebase.Pages
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            BindingContext = Services.ServiceProvider.GetService<AppShellViewModel>();

            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(RideEndedPage), typeof(RideEndedPage));
            Routing.RegisterRoute(nameof(RideHistoryPage), typeof(RideHistoryPage));
            Routing.RegisterRoute(nameof(RideProgressPage), typeof(RideProgressPage));
            Routing.RegisterRoute(nameof(RideSelectionPage), typeof(RideSelectionPage));

            // Support route
            Routing.RegisterRoute(nameof(SupportPage), typeof(SupportPage));

            // Onboarding routes
            Routing.RegisterRoute(nameof(OnboardingProfilePage), typeof(OnboardingProfilePage));
            Routing.RegisterRoute(nameof(OnboardingRolePage), typeof(OnboardingRolePage));
            Routing.RegisterRoute(nameof(OnboardingDriverPage), typeof(OnboardingDriverPage));

            // Driver routes
            Routing.RegisterRoute(nameof(DriverDashboardPage), typeof(DriverDashboardPage));
            Routing.RegisterRoute(nameof(DriverCounterOfferPage), typeof(DriverCounterOfferPage));
            Routing.RegisterRoute(nameof(DriverProfilePage), typeof(DriverProfilePage));
            Routing.RegisterRoute(nameof(DriverRideProgressPage), typeof(DriverRideProgressPage));
            Routing.RegisterRoute(nameof(DriverSosPage), typeof(DriverSosPage));
            Routing.RegisterRoute(nameof(DriverStatsPage), typeof(DriverStatsPage));
        }
    }
}
