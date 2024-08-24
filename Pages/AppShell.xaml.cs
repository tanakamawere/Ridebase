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

            Routing.RegisterRoute(nameof(MapHomePage), typeof(MapHomePage));
        }
    }
}
