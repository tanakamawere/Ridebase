namespace Ridebase.Models;

public class PlacesModels
{
}

public class DisplayName
{
    public string text { get; set; }
    public string languageCode { get; set; }
}

public class Place
{
    public string id { get; set; }
    public List<string> types { get; set; }
    public string formattedAddress { get; set; }
    public Location location { get; set; }
    public DisplayName displayName { get; set; }
}

public class PlacesRoot
{
    public List<Place> places { get; set; }
}

