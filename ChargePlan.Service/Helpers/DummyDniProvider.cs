using ChargePlan.Weather;

namespace ChargePlan.Service.Helpers;

/// <summary>
/// Dummy DNI which has zero diffuse Watts and 1,000 direct Watts, so that the shading can easily be determined.
/// </summary>
public class DummyDniProvider : IDirectNormalIrradianceProvider
{
    private readonly DateTimeOffset _startDate;
    public DummyDniProvider(DateTimeOffset earliestDateToConsider)
    {
        _startDate = earliestDateToConsider;
    }

    public Task<IEnumerable<DniValue>> GetDniForecastAsync()
    {
        IEnumerable<DniValue> Values()
        {
            DateTimeOffset date = _startDate;
            while (date < _startDate.AddDays(7))
            {
                yield return new DniValue(date, 1000.0f, 0.0f, 0);
                date += TimeSpan.FromHours(1);
            }
        }
        return Task.FromResult(Values());
    }
}