using Ridebase.Models;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Pages.Onboarding;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Onboarding;

public partial class OnboardingRoleViewModel : BaseViewModel
{
    public OnboardingRoleViewModel(IUserSessionService _userSessionService)
    {
        userSessionService = _userSessionService;
        Title = "Choose Your Role";
    }

    [RelayCommand]
    public async Task ChooseDriverAsync()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Driver);
        await Shell.Current.GoToAsync(nameof(OnboardingDriverPage));
    }

    [RelayCommand]
    public async Task ChooseRiderAsync()
    {
        await userSessionService.SetRoleAsync(AppUserRole.Rider);
        await userSessionService.SetOnboardedAsync(true);
        await Shell.Current.GoToAsync("//Home");
    }
}
