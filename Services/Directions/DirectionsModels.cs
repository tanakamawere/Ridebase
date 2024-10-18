namespace Ridebase.Services.Directions;

public class DirectionsModels
{
}
public class Bounds
{
    public DirectionsLocation northeast { get; set; }
    public DirectionsLocation southwest { get; set; }
}

public class Distance
{
    public string text { get; set; }
    public int value { get; set; }
}

public class Duration
{
    public string text { get; set; }
    public int value { get; set; }
}

public class GeocodedWaypoint
{
    public string geocoder_status { get; set; }
    public string place_id { get; set; }
    public List<string> types { get; set; }
}

public class Leg
{
    public Distance distance { get; set; }
    public Duration duration { get; set; }
    public string end_address { get; set; }
    public DirectionsLocation end_location { get; set; }
    public string start_address { get; set; }
    public DirectionsLocation start_location { get; set; }
    public List<Step> steps { get; set; }
    public List<object> traffic_speed_entry { get; set; }
    public List<object> via_waypoint { get; set; }
}

public class OverviewPolyline
{
    public string points { get; set; }
}

public class Polyline
{
    public string points { get; set; }
}

public class DirectionsRoot
{
    public List<GeocodedWaypoint> geocoded_waypoints { get; set; }
    public List<Route> routes { get; set; }
    public string status { get; set; }
}

public class Route
{
    public Bounds bounds { get; set; }
    public string copyrights { get; set; }
    public List<Leg> legs { get; set; }
    public OverviewPolyline overview_polyline { get; set; }
    public string summary { get; set; }
    public List<object> warnings { get; set; }
    public List<int> waypoint_order { get; set; }
}

public class Step
{
    public Distance distance { get; set; }
    public Duration duration { get; set; }
    public DirectionsLocation end_location { get; set; }
    public string html_instructions { get; set; }
    public Polyline polyline { get; set; }
    public DirectionsLocation start_location { get; set; }
    public string travel_mode { get; set; }
    public string maneuver { get; set; }
}

public class DirectionsLocation
{
    public double lat { get; set; }
    public double lng { get; set; }
}


