public interface IDirectNormalIrradianceProvider
{
    public IEnumerable<(DateTime DateTime, float PowerWatts)> GetForecast();
}