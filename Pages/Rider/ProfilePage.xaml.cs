using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    private async void OnDriverModeToggled(object? sender, ToggledEventArgs e)
    {
        if (!e.Value)
        {
            return;
        }

        await viewModel.SwitchToDriverModeCommand.ExecuteAsync(null);
        if (sender is Switch driverSwitch)
        {
            driverSwitch.IsToggled = false;
        }
    }
}
