namespace ChargePlan.Weather;

public static class Sol
{
    /// <summary>
    /// In Radians.
    /// Note that the plane elevation at right-angles to the Sun will produce maximum. I.e. a plane elevation of 0 radians (laying on the ground)
    /// with the Sun directly overhead will produce a maximum.
    /// </summary>
    public static double DniToIrradiation(double dni, double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation, double? diffuseIrradiation)
    {
        // Not much light if it's below the horizon...
        if (sunElevation < 0) return diffuseIrradiation ?? 0.0;

        double azimuth = sunAzimith - planeAzimuth;
        double elevation = sunElevation - planeElevation;

        return Math.Max(diffuseIrradiation ?? 0.0, dni * Math.Max(0.0f, Math.Cos(azimuth)) * Math.Max(0.0f, Math.Sin(Math.Abs(elevation))));
    }

    public static (double Altitude, double Azimuth) SunPositionRads(DateTimeOffset dateTime, float latitudeDegrees, float longitudeDegrees)
    {
        // double latRadians = latitudeDegrees * Math.PI / 180.0;
        // double declination = DeclinationRads(dateTime);
        // double hourAngle = HourAngleRads(dateTime);

        // double altitude = Math.Asin(Math.Sin(declination) * Math.Sin(latRadians) + Math.Cos(declination) * Math.Cos(latRadians) * Math.Cos(hourAngle));
        // double azimuth = Math.Atan2(Math.Sin(hourAngle) * Math.Cos(declination) - Math.Sin(declination) * Math.Cos(latRadians) * Math.Cos(hourAngle), Math.Cos(latRadians) * Math.Sin(hourAngle));

        // altitude = 360 * altitude / (2.0 * Math.PI);
        // azimuth = 360 * azimuth / (2.0 * Math.PI);;

        // return (altitude, azimuth);
        var result = SunCalcSharp.SunCalc.GetPosition(dateTime.UtcDateTime, latitudeDegrees, longitudeDegrees);

        return (result.Altitude, result.Azimuth);
    }

    public static float ToDegrees(this double rads) => (float)(rads * 180.0 / Math.PI);
    public static double ToRads(this float degrees) => (double)degrees * Math.PI / 180.0;
}