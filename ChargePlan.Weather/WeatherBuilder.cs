using ChargePlan.Domain;
using ChargePlan.Domain.Exceptions;

namespace ChargePlan.Weather;

/// <summary>
/// 
/// </summary>
/// <param name="FetchGenerationProfile"></param>
/// <param name="PanelElevation">Degrees</param>
/// <param name="PanelAzimuth">Degrees. Zero is South.</param>
/// <param name="Latitude">Degrees</param>
/// <param name="Longitude">Degrees</param>
/// <param name="IrradianceToPowerScalar"></param>
/// <param name="ShadingPolygons"></param>
public record WeatherBuilder(Func<WeatherBuilder, Task<IGenerationProfile>> FetchGenerationProfile, float PanelElevation, float PanelAzimuth, float Latitude, float Longitude, float IrradianceToPowerScalar, IEnumerable<Shading> ShadingPolygons)
{
    public WeatherBuilder(float PanelElevation, float PanelAzimuth, float Latitude, float Longitude) : this(Unset, PanelElevation, PanelAzimuth, Latitude, Longitude, 1.0f, Enumerable.Empty<Shading>()) { }

    private static Task<IGenerationProfile> Unset(WeatherBuilder _) => throw new InvalidStateException("Please add a weather source");

    public WeatherBuilder WithDniSource(IDirectNormalIrradianceProvider dni)
        => this with
        {
            FetchGenerationProfile = async (WeatherBuilder wb) => new GenerationProfile()
            {
                Values = (await dni.GetForecastAsync()).Select(f =>
                {
                    var sun = Sol.SunPositionRads(f.DateTime, wb.Latitude, wb.Longitude);

                    if (wb.ShadingPolygons.Any(f => f.IsSunPositionShaded((sun.Altitude.ToDegrees(), sun.Azimuth.ToDegrees()))))
                    {
                        return new GenerationValue(f.DateTime.ToLocalTime(), 0.0f);
                    }

                    double irradiatedPower = Sol.DniToIrradiation(f.PowerWatts, wb.PanelAzimuth.ToRads(), wb.PanelElevation.ToRads(), sun.Azimuth, sun.Altitude);
                    return new GenerationValue(f.DateTime.ToLocalTime(), wb.IrradianceToPowerScalar * (float)irradiatedPower / 1000.0f);
                })
            }
        };

    public WeatherBuilder WithArrayArea(float areaSqm, float efficiencyPercent = 20.5f)
        => this with { IrradianceToPowerScalar = areaSqm * efficiencyPercent / 100.0f };

    public WeatherBuilder AddShading(Shading shading)
        => this with { ShadingPolygons = ShadingPolygons.Append(shading) };

    public WeatherBuilder AddShading(IEnumerable<Shading> shading)
        => this with { ShadingPolygons = ShadingPolygons.Concat(shading) };

    public WeatherBuilder AddShadingToHorizon(float degreesAltitude) => AddShading(new Shading((0, -180), (degreesAltitude, -180), (degreesAltitude, 180), (0, 180)));

    public Task<IGenerationProfile> BuildAsync() => FetchGenerationProfile(this);
}