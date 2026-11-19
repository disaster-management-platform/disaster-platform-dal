using System.Text.Json;

namespace DisasterLocation.Core;

public sealed record DisasterArea(double Latitude, double Longitude, double? Radius, JsonElement? Polygon)
{
    public static DisasterArea? FromPoints(IEnumerable<(double Latitude, double Longitude)> source)
    {
        var points = source.Distinct().ToArray();
        if (points.Length == 0) return null;
        if (points.Any(p => !double.IsFinite(p.Latitude) || !double.IsFinite(p.Longitude)
            || p.Latitude is < -90 or > 90 || p.Longitude is < -180 or > 180))
            throw new ArgumentOutOfRangeException(nameof(source), "Coordenadas inválidas.");
        var sorted = points.OrderBy(p => p.Longitude).ThenBy(p => p.Latitude).ToArray();
        var hull = new List<(double Latitude, double Longitude)>();
        foreach (var p in sorted)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], p) <= 1e-12) hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        var lower = hull.Count;
        foreach (var p in sorted.Reverse().Skip(1))
        {
            while (hull.Count > lower && Cross(hull[^2], hull[^1], p) <= 1e-12) hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
        var center = points[0];
        JsonElement? polygon = null;
        if (hull.Count >= 3)
        {
            var origin = hull[0];
            double area = 0, x = 0, y = 0;
            for (var i = 0; i < hull.Count; i++)
            {
                var a = hull[i]; var b = hull[(i + 1) % hull.Count];
                var cross = (a.Longitude - origin.Longitude) * (b.Latitude - origin.Latitude)
                    - (b.Longitude - origin.Longitude) * (a.Latitude - origin.Latitude);
                area += cross;
                x += (a.Longitude + b.Longitude - 2 * origin.Longitude) * cross;
                y += (a.Latitude + b.Latitude - 2 * origin.Latitude) * cross;
            }
            center = (origin.Latitude + y / (3 * area), origin.Longitude + x / (3 * area));
            if (hull.Count + 1 > 2000) throw new ArgumentOutOfRangeException(nameof(source), "El polígono supera 2000 posiciones.");
            polygon = JsonSerializer.SerializeToElement(new { type = "Polygon", coordinates = new[] {
                hull.Append(hull[0]).Select(p => new[] { p.Longitude, p.Latitude }).ToArray() } });
        }
        var radius = points.Max(p => Distance(center, p));
        if (radius > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "El área supera el radio máximo de 1.000.000 m.");
        return new(center.Latitude, center.Longitude, radius > 0 ? radius : null, polygon);
    }

    private static double Cross((double Latitude, double Longitude) a, (double Latitude, double Longitude) b,
        (double Latitude, double Longitude) c) => (b.Longitude - a.Longitude) * (c.Latitude - a.Latitude)
            - (b.Latitude - a.Latitude) * (c.Longitude - a.Longitude);
    private static double Distance((double Latitude, double Longitude) a, (double Latitude, double Longitude) b)
    {
        const double radians = Math.PI / 180;
        var value = Math.Pow(Math.Sin((b.Latitude - a.Latitude) * radians / 2), 2)
            + Math.Cos(a.Latitude * radians) * Math.Cos(b.Latitude * radians)
            * Math.Pow(Math.Sin((b.Longitude - a.Longitude) * radians / 2), 2);
        return 6_371_008.8 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(value, 0, 1)));
    }
}
