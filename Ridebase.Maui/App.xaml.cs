using Ridebase.Pages;
using Ridebase.Services;

namespace Ridebase;

public partial class App : Application
{
    public App(OidcLoginService oidcLoginService)
    {
        InitializeComponent();

        // Pre-warm TLS + OIDC discovery in the background so first login is faster
        _ = oidcLoginService.PreWarmAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
