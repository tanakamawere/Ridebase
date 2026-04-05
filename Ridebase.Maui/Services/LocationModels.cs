namespace Ridebase.Services;

public enum LocationAcquisitionStatus
{
    Success,
    PermissionDenied,
    Unavailable,
    NotSupported,
    Error
}

public sealed class LocationAcquisitionResult
{
    public LocationAcquisitionStatus Status { get; init; }
    public Microsoft.Maui.Devices.Sensors.Location? DeviceLocation { get; init; }
    public string? ErrorMessage { get; init; }
}

public class LocationWithAddress
{
    public Models.Location Location { get; set; } = new();
    public string FormattedAddress { get; set; } = string.Empty;
}
