using ChargePlan.Domain;

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
public record WeatherBuilder(IDirectNormalIrradianceProvider? DniProvider, float PanelElevation, float PanelAzimuth, float Latitude, float Longitude, float IrradianceToPowerScalar, int? AbsolutePeakWatts, float SunlightScalar, IEnumerable<Shading> ShadingPolygons)
{
    public WeatherBuilder(float PanelElevation, float PanelAzimuth, float Latitude, float Longitude) : this(null, PanelElevation, PanelAzimuth, Latitude, Longitude, 1.0f, null, 1.0f, Enumerable.Empty<Shading>()) { }

    public WeatherBuilder WithDniSource(IDirectNormalIrradianceProvider dni)
        => this with { DniProvider = dni };

    public WeatherBuilder WithArrayArea(float areaSqm, float efficiencyPercent = 20.5f, int? absolutePeakWatts = null)
        => this with { IrradianceToPowerScalar = areaSqm * efficiencyPercent / 100.0f, AbsolutePeakWatts = absolutePeakWatts };

    public WeatherBuilder AddShading(Shading shading)
        => this with { ShadingPolygons = ShadingPolygons.Append(shading) };

    public WeatherBuilder AddShading(IEnumerable<Shading> shading)
        => this with { ShadingPolygons = ShadingPolygons.Concat(shading) };

    public WeatherBuilder WithForecastSettings(float sunlightScalar = 1.0f)
        => this with { SunlightScalar = sunlightScalar };

    public WeatherBuilder AddShadingToHorizon(float degreesAltitude) => AddShading(new Shading((0, -180), (degreesAltitude, -180), (degreesAltitude, 180), (0, 180)));

    public async Task<IGenerationProfile> BuildAsync()
    {
        var generation = Enumerable.Empty<GenerationValue>();

        if (DniProvider != null)
        {
            generation = (await DniProvider.GetDniForecastAsync()).Select(f =>
            {
                var sun = Sol.SunPositionRads(f.DateTime, Latitude, Longitude);

                double irradiatedPower = Sol.DniToIrradiation(f.DirectWatts, PanelAzimuth.ToRads(), PanelElevation.ToRads(), sun.Azimuth, sun.Altitude, f.DiffuseWatts);
                irradiatedPower *= SunlightScalar;

                if (irradiatedPower > 0.0 && ShadingPolygons.Any(p => p.IsSunPositionShaded((sun.Altitude.ToDegrees(), sun.Azimuth.ToDegrees()))))
                {
                    irradiatedPower = f.DiffuseWatts ?? 0.0f;
                }

                float kw = IrradianceToPowerScalar * (float)irradiatedPower / 1000.0f;
                kw = Math.Min(kw, (AbsolutePeakWatts ?? int.MaxValue) / 1000.0f);

                return new GenerationValue(f.DateTime.ToLocalTime(), kw);
            });
        }
        else
        {
            throw new InvalidOperationException("Please supply a weather forecast provider e.g. call WithDniSource");
        }

        return new GenerationProfile() { Values = generation };
    }
}