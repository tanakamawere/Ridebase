using Ridebase.ViewModels;
using System.Globalization;

namespace Ridebase.Converters;

/// <summary>
/// Returns true when the current SearchState equals the converter parameter.
/// Usage: IsVisible="{Binding CurrentSearchState, Converter={StaticResource SearchStateEquals}, ConverterParameter=Idle}"
/// </summary>
public class SearchStateEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SearchState state && parameter is string name)
        {
            return Enum.TryParse<SearchState>(name, out var target) && state == target;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the current SearchState does NOT equal the converter parameter.
/// Usage: IsVisible="{Binding CurrentSearchState, Converter={StaticResource SearchStateNotEqual}, ConverterParameter=Idle}"
/// </summary>
public class SearchStateNotEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SearchState state && parameter is string name)
        {
            return Enum.TryParse<SearchState>(name, out var target) && state != target;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the current SearchField equals the converter parameter.
/// Usage: Stroke="{Binding ActiveSearchField, Converter={StaticResource SearchFieldEquals}, ConverterParameter=Pickup}"
/// </summary>
public class SearchFieldEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SearchField field && parameter is string name)
        {
            return Enum.TryParse<SearchField>(name, out var target) && field == target;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns an active or inactive color based on whether SearchField matches the parameter.
/// Active = Primary color, Inactive = Gray300/Gray600 (theme-aware).
/// </summary>
public class SearchFieldToStrokeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = false;
        if (value is SearchField field && parameter is string name)
        {
            isActive = Enum.TryParse<SearchField>(name, out var target) && field == target;
        }

        if (isActive)
            return Application.Current!.Resources.TryGetValue("Primary", out var primary)
                ? (Color)primary : Colors.Purple;

        // Inactive — use theme-aware gray
        if (Application.Current!.RequestedTheme == AppTheme.Dark)
            return Application.Current.Resources.TryGetValue("Gray600", out var dark)
                ? (Color)dark : Colors.DarkGray;

        return Application.Current.Resources.TryGetValue("Gray200", out var light)
            ? (Color)light : Colors.LightGray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
