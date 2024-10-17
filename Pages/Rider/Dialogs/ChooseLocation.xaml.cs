using Mopups.Services;
using Ridebase.Services.Geocoding;
using Ridebase.ViewModels;
using System.Threading.Tasks;

namespace Ridebase.Pages.Rider.Dialogs;

public partial class ChooseLocation 
{
    TaskCompletionSource<Result> _taskCompletionSource;
    public Task<Result> PopupDismissedTask => _taskCompletionSource.Task;
    public Result ReturnValue { get; set; }
    public ChooseLocation(ChooseGoToLocationViewModel chooseGoToLocationViewModel)
	{
		InitializeComponent();
        BindingContext = chooseGoToLocationViewModel;
    }
    private void Button_Clicked(object sender, EventArgs e)
    {
        MopupService.Instance.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _taskCompletionSource = new TaskCompletionSource<Result>();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _taskCompletionSource.SetResult(ReturnValue);
    }

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection is null) return;

        Result response = (Result)e.CurrentSelection[0];
    }
}