using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ridebase.Models;
using Ridebase.Services.Interfaces;

namespace Ridebase.ViewModels.Driver;

public partial class DriverProfileViewModel : BaseViewModel
{
    public DriverProfileViewModel(ILogger<DriverProfileViewModel> logger, IUserSessionService _userSessionService)
    {
        Logger = logger;
        userSessionService = _userSessionService;
    }

    [RelayCommand]
    public async Task GoToRiderPages()
    {
        Logger.LogInformation("Switching to Rider Pages");
        await userSessionService.SetRoleAsync(AppUserRole.Rider);
        await Shell.Current.GoToAsync("//Home");
    }
}