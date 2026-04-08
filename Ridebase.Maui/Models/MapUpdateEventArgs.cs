namespace Ridebase.Models;

public enum MapUpdateType
{
    Camera,
    Route,
    Clear
}

public class MapUpdateEventArgs : EventArgs
{
    public MapUpdateType Type { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Zoom { get; set; }
    public string? RoutePolyline { get; set; }
}
