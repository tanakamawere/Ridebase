using CommunityToolkit.Mvvm.Input;
using Ridebase.Pages;

namespace Ridebase.ViewModels.Driver;

public partial class DriverProfileViewModel : BaseViewModel
{
    [RelayCommand]
    //Method to go to AppShell and pop the stack
    public void GoToRiderPages()
    {
        Application.Current.OpenWindow(new Window(new AppShell()));
    }
}