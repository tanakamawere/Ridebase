using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ridebase.Models;
using Microsoft.Maui.Devices.Sensors;
using Ridebase.Services;

namespace Ridebase.ViewModels;

public partial class MapHomeViewModel : BaseViewModel
{
    public MapHomeViewModel()
    {
        Title = "Map Page";
    }
}
