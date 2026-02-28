using CommunityToolkit.Mvvm.Input;
using Ridebase.Pages.Onboarding;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingRoleViewModel : BaseViewModel
{
    public OnboardingRoleViewModel()
    {
        Title = "Choose Your Role";
    }

    [RelayCommand]
    public async Task ChooseDriverAsync()
    {
        await Shell.Current.GoToAsync(nameof(OnboardingDriverPage));
    }

    [RelayCommand]
    public async Task ChooseRiderAsync()
    {
        await Shell.Current.GoToAsync("//Home");
    }
}
