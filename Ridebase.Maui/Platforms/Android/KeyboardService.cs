using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using Ridebase.Platforms.Android;
using Ridebase.Services;

[assembly: Dependency(typeof(KeyboardService))]
namespace Ridebase.Platforms.Android;

public class KeyboardService : IKeyboardService
{
    private bool _isKeyboardVisible = false;
    private Activity _activity;
    public event EventHandler<bool> KeyboardStateChanged;

    public KeyboardService()
    {
        _activity = Platform.CurrentActivity;
        _activity.Window.DecorView.ViewTreeObserver.GlobalLayout += OnGlobalLayout;
    }

    private void OnGlobalLayout(object sender, EventArgs e)
    {
        InputMethodManager imm = (InputMethodManager)_activity.GetSystemService(Context.InputMethodService);
        if (imm != null && _activity.CurrentFocus != null)
        {
            var isKeyboardNowVisible = imm.IsAcceptingText;

            // Only invoke event if the keyboard visibility status has changed
            if (isKeyboardNowVisible != _isKeyboardVisible)
            {
                _isKeyboardVisible = isKeyboardNowVisible;
                KeyboardStateChanged?.Invoke(this, _isKeyboardVisible);
            }
        }
    }
}
