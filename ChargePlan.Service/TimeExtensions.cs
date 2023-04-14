public static class TimeExtensions
{
    /// <summary>
    /// Calculate energy at a specified rate of power for this duration.
    /// </summary>
    public static float Energy(this TimeSpan ts, float Power) => Power * (float)ts.TotalHours;
}