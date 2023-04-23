public static class TimeExtensions
{
    /// <summary>
    /// Calculate energy at a specified rate of power for this duration.
    /// </summary>
    public static float Energy(this TimeSpan ts, float Power) => Power * (float)ts.TotalHours;

    /// <summary>
    /// Represent a datetime as a fractional number of hours since mindate.
    /// </summary>
    public static double AsTotalHours(this DateTime dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;

    /// <summary>
    /// Align to the closest previous hour
    /// </summary>
    public static DateTime ToClosestHour(this DateTime dateTime) => new DateTime(dateTime.Ticks / TimeSpan.FromHours(1).Ticks);

    public static DateTime Tomorrow(this DateTime dateTime) => dateTime.Date.AddDays(1);
}