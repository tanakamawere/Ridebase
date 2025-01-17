using System.Globalization;

namespace Ridebase.Converters;

public class LocationToPointConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.Location location)
        {
            return new Point(location.latitude, location.longitude);
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Point point)
        {
            return new Models.Location() { latitude = point.X, longitude = point.Y };
        }

        return value;
    }
}