namespace ChargePlan.Weather;

/// <summary>
/// Shading where panels cannot see Sun.
/// </summary>
/// <param name="Points">Points in degrees defining shading. Zero Azimuth is South.</param>
public record Shading(IEnumerable<(float Altitude, float Azimuth)> Points)
{
    public Shading() : this(Enumerable.Empty<(float, float)>()) { }
    public Shading(params (float Altitude, float Azimuth)[] points) : this((IEnumerable<(float Altitude, float Azimuth)>)points) { }

    /// <summary>
    /// Shading needs to have 0 Azimuth as South.
    /// If you created it using some other origin, then rotate using this function.
    /// </summary>
    public Shading WithAzimuthRotatedBy(float degrees)
        => this with { Points = Points.Select(f => (f.Altitude, f.Azimuth + degrees)).ToArray() };

    public bool IsSunPositionShaded((float Altitude, float Azimuth) point)
    {
        var polygon = Points.ToArray();

        if (polygon.Length == 0) return false;

        int polygonLength = polygon.Length, i = 0;
        bool inside = false;
        // x, y for tested point.
        float pointX = point.Azimuth, pointY = point.Altitude;
        // start / end point for the current polygon segment.
        float startX, startY, endX, endY;
        var endPoint = polygon[polygonLength - 1];
        endX = endPoint.Azimuth;
        endY = endPoint.Altitude;
        while (i < polygonLength)
        {
            startX = endX; startY = endY;
            endPoint = polygon[i++];
            endX = endPoint.Azimuth; endY = endPoint.Altitude;

            //if ((startY - endY) == 0.0f) throw new InvalidOperationException($"Cannot calculate Sun position shading because start and end points are the same. {String.Join(" ", Points.Select(f=>$"({f.Altitude},{f.Azimuth})"))}");
            //
            inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
            && /* if so, test if it is under the segment */
            ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
        }
        return inside;
    }
}
