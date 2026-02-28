namespace Ridebase.Models;

public class UserBootstrapState
{
    public string UserId { get; set; } = string.Empty;
    public AppUserRole Role { get; set; } = AppUserRole.Rider;
    public bool IsOnboarded { get; set; }
    public bool IsDriverSubscribed { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
