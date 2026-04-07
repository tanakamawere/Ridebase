using Ridebase.ViewModels.Onboarding;

namespace Ridebase.Pages.Onboarding;

public partial class OnboardingOtpPage : ContentPage
{
    public OnboardingOtpPage(OnboardingOtpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
