using Foundation;
using Ridebase.Platforms.iOS;
using Ridebase.Services;
using UIKit;

[assembly: Dependency(typeof(KeyboardService))]
namespace Ridebase.Platforms.iOS;

public class KeyboardService : IKeyboardService
{
    public event EventHandler<bool> KeyboardStateChanged;

    public KeyboardService()
    {
        NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardShow);
        NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardHide);
    }

    private void OnKeyboardShow(NSNotification notification)
    {
        KeyboardStateChanged?.Invoke(this, true); // Keyboard is open
    }

    private void OnKeyboardHide(NSNotification notification)
    {
        KeyboardStateChanged?.Invoke(this, false); // Keyboard is closed
    }
}
