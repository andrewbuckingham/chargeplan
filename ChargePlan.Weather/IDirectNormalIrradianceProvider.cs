namespace ChargePlan.Weather;

public interface IDirectNormalIrradianceProvider
{
    public Task<IEnumerable<DniValue>> GetDniForecastAsync();
}

/// <summary>
/// A reading from the DNI forecast provider for a point in time.
/// </summary>
/// <param name="DateTime">Date and time of the forecast.</param>
/// <param name="DirectWatts">Watts of sunlight onto direct normal surface.</param>
/// <param name="DiffuseWatts">Watts of sunlight from diffuse sky (i.e. shaded surface). Null if unknown.</param>
/// <param name="CloudCoverPercent">Cloud cover percentage at the time of the estimate. Null if unknown.</param>
public record DniValue(
    DateTime DateTime,
    float DirectWatts,
    float? DiffuseWatts,
    int? CloudCoverPercent
);
