using Ridebase.ViewModels.Onboarding;

namespace Ridebase.Pages.Onboarding;

public partial class OnboardingRolePage : ContentPage
{
    private readonly OnboardingRoleViewModel onboardingRoleViewModel;

    public OnboardingRolePage(OnboardingRoleViewModel _viewModel)
    {
        InitializeComponent();
        onboardingRoleViewModel = _viewModel;
        BindingContext = onboardingRoleViewModel;
    }
}
