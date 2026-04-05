using Ridebase.ViewModels.Onboarding;

namespace Ridebase.Pages.Onboarding;

public partial class OnboardingDriverPage : ContentPage
{
    private readonly OnboardingDriverViewModel onboardingDriverViewModel;

    public OnboardingDriverPage(OnboardingDriverViewModel _viewModel)
    {
        InitializeComponent();
        onboardingDriverViewModel = _viewModel;
        BindingContext = onboardingDriverViewModel;
    }
}
