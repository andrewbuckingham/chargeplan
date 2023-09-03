namespace ChargePlan.Service.Entities.ForecastTuning;

/// <summary>
/// Settings to use when tuning the forecast.
/// </summary>
/// <param name="ForecastLengthToOptimiseFor">Scalar should optimise for correcting the forecast at this distance in the future.</param>
/// <param name="PeriodToAverageOver">What rolling period to average over. Longer gives more stability, shorter reacts more quickly.</param>
/// <param name="IgnoreEnergiesBelow">Don't bother if the actual energy was below 0.1kWh</param>
public record ForecastTuningSettings(
    TimeSpan ForecastLengthToOptimiseFor,
    TimeSpan PeriodToAverageOver,
    float IgnoreEnergiesBelow
)
{
    public ForecastTuningSettings() : this(
        ForecastLengthToOptimiseFor: TimeSpan.FromHours(24),
        PeriodToAverageOver: TimeSpan.FromDays(7),
        IgnoreEnergiesBelow: 0.1f
    ) { }

    /// <summary>
    /// Total history to store per forecast entry. This must equal or exceed ForecastLengthToOptimiseFor.
    /// </summary>
    public static readonly TimeSpan MaximumForecastLengthToStore = TimeSpan.FromHours(24);
}
