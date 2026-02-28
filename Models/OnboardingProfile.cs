namespace Ridebase.Models;

public class OnboardingProfile
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string City { get; set; }
    public bool DefaultLocationPermissionGranted { get; set; }
    public bool ProfileConfirmed { get; set; }
}
