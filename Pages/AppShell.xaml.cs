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
            Routing.RegisterRoute(nameof(RideDetailsPage), typeof(RideDetailsPage));
            Routing.RegisterRoute(nameof(RideEndedPage), typeof(RideEndedPage));
            Routing.RegisterRoute(nameof(RideHistoryPage), typeof(RideHistoryPage));
            Routing.RegisterRoute(nameof(RideProgressPage), typeof(RideProgressPage));
            Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
        }
    }
}
