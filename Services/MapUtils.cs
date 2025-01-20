using MPowerKit.GoogleMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ridebase.Services;

public static class MapUtils
{
    public static LatLngBounds CalculateBounds(PointCollection polylinePoints)
    {
        if (polylinePoints == null || polylinePoints.Count == 0)
            throw new ArgumentException("Polyline points cannot be null or empty");

        // Initialize min and max values
        double minLat = double.MaxValue, minLon = double.MaxValue;
        double maxLat = double.MinValue, maxLon = double.MinValue;

        foreach (var point in polylinePoints)
        {
            if (point.X < minLat) minLat = point.X; // Latitude
            if (point.X > maxLat) maxLat = point.X;

            if (point.Y < minLon) minLon = point.Y; // Longitude
            if (point.Y > maxLon) maxLon = point.Y;
        }

        // Create the bounds from the calculated values
        var southWest = new Point(minLat, minLon);
        var northEast = new Point(maxLat, maxLon);

        return new LatLngBounds(southWest, northEast);
    }
}
