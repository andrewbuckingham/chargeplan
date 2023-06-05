namespace ChargePlan.Domain;

public static class TimeExtensions
{
    /// <summary>
    /// Calculate energy at a specified rate of power for this duration.
    /// </summary>
    public static float Energy(this TimeSpan ts, float Power) => Power * (float)ts.TotalHours;

    /// <summary>
    /// Calculate average power to have produced this amount of energy over this duration.
    /// </summary>
    public static float Power(this TimeSpan ts, float Energy) => Energy / (float)ts.TotalHours;

    /// <summary>
    /// Represent a datetime as a fractional number of hours since mindate.
    /// </summary>
    public static double AsTotalHours(this DateTime dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;

    /// <summary>
    /// Represent a datetime as a fractional number of hours since mindate.
    /// </summary>
    public static double AsTotalHours(this DateTimeOffset dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;

    /// <summary>
    /// Align to the closest previous hour
    /// </summary>
    public static DateTimeOffset ToClosestHour(this DateTimeOffset dateTime) => new DateTimeOffset(
        DateTime.MinValue.AddHours(dateTime.Ticks / TimeSpan.FromHours(1).Ticks),
        dateTime.Offset);

    /// <summary>
    /// If this value is earlier than the supplied, then move it forward
    /// </summary>
    public static DateTimeOffset OrAtEarliest(this DateTimeOffset dateTime, DateTimeOffset earliestAllowedDate)
        => dateTime.Ticks < earliestAllowedDate.Ticks ? earliestAllowedDate : dateTime;
}