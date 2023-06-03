namespace ChargePlan.Weather;

public static class Sol
{
    /// <summary>
    /// In Radians.
    /// </summary>
    public static double DniToIrradiation(double dni, double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation)
    {
        // Not much light if it's below the horizon...
        if (sunElevation < 0) return 0.0;

        double azimuth = sunAzimith - planeAzimuth;
        double elevation = sunElevation - planeElevation;

        // Likewise if it's past normal to the panel front face.
        if (Math.Abs(azimuth) > Math.PI / 2) return 0.0;
        if (Math.Abs(elevation) > Math.PI / 2) return 0.0;

        return Math.Max(0.0, dni * Math.Cos(azimuth) * Math.Cos(elevation));
    }

    public static (double Altitude, double Azimuth) SunPositionRads(DateTime dateTime, double latitudeDegrees, double longitudeDegrees)
    {
        // double latRadians = latitudeDegrees * Math.PI / 180.0;
        // double declination = DeclinationRads(dateTime);
        // double hourAngle = HourAngleRads(dateTime);

        // double altitude = Math.Asin(Math.Sin(declination) * Math.Sin(latRadians) + Math.Cos(declination) * Math.Cos(latRadians) * Math.Cos(hourAngle));
        // double azimuth = Math.Atan2(Math.Sin(hourAngle) * Math.Cos(declination) - Math.Sin(declination) * Math.Cos(latRadians) * Math.Cos(hourAngle), Math.Cos(latRadians) * Math.Sin(hourAngle));

        // altitude = 360 * altitude / (2.0 * Math.PI);
        // azimuth = 360 * azimuth / (2.0 * Math.PI);;

        // return (altitude, azimuth);
        var result = SunCalcSharp.SunCalc.GetPosition(dateTime, latitudeDegrees, longitudeDegrees);

        return (result.Altitude, result.Azimuth);
    }

    public static double ToDegrees(this double rads) => rads * 180.0 / Math.PI;
    public static double ToRads(this double degrees) => degrees * Math.PI / 180;
}