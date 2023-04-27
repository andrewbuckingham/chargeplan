public interface IDirectNormalIrradianceProvider
{
    public Task<IEnumerable<(DateTime DateTime, float PowerWatts)>> GetForecastAsync();
}