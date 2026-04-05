using Ridebase.ViewModels.Rider;

namespace Ridebase.Pages.Rider;

public partial class WalletPage : ContentPage
{
    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
