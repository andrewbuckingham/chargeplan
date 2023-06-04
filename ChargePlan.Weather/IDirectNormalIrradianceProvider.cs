namespace ChargePlan.Weather;

public interface IDirectNormalIrradianceProvider
{
    public Task<IEnumerable<(DateTime DateTime, float DirectWatts, float? DiffuseWatts)>> GetDniForecastAsync();
}