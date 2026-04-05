using Ridebase.ViewModels.Onboarding;

namespace Ridebase.Pages.Onboarding;

public partial class OnboardingProfilePage : ContentPage
{
    private readonly OnboardingProfileViewModel onboardingProfileViewModel;

    public OnboardingProfilePage(OnboardingProfileViewModel _viewModel)
    {
        InitializeComponent();
        onboardingProfileViewModel = _viewModel;
        BindingContext = onboardingProfileViewModel;
    }
}
