using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain;

namespace ChargePlan.Service.Entities;

public class ForecastHistory
{
    public List<ForecastDatapoint> Values = new();

    /// <summary>
    /// E.g. if the forecastHorizon is 4hrs, then this will obtain all the ForHour values where
    /// the forecast was predicted at least 4hrs earlier.
    /// </summary>
    public IEnumerable<ForecastDatapoint> GetHourlyForecastsForHorizon(TimeSpan forecastHorizon)
        => Values
            .Where(f=>f.ForecastLength >= forecastHorizon)
            .Select(f=>f with { ForHour = f.ForHour.ToClosestHour() })
            .ToLookup(f=>f.ForHour)
            .Select(f=>f.OrderBy(f=>f.ForecastLength).First());
}

/// <summary>
/// A forecast for a point in time.
/// </summary>
/// <param name="ForHour"></param>
/// <param name="ProducedAt"></param>
/// <param name="Energy">kWh energy produced in the hour</param>
public record ForecastDatapoint(
    DateTimeOffset ForHour,
    DateTimeOffset ProducedAt,
    float Energy,
    int CloudCoverPercent)
{
    public TimeSpan ForecastLength =>
        ProducedAt <= ForHour
        ? ForHour - ProducedAt
        : throw new InvalidStateException("Forecast is for a time in the past!");
};
