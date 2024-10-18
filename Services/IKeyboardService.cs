namespace Ridebase.Services;

public interface IKeyboardService
{
    event EventHandler<bool> KeyboardStateChanged;
}

