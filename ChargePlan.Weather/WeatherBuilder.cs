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
public record WeatherBuilder(IDirectNormalIrradianceProvider? DniProvider, float PanelElevation, float PanelAzimuth, float Latitude, float Longitude, float IrradianceToPowerScalar, int? AbsolutePeakWatts, float SunlightScalar, TimeSpan TimeStep, IEnumerable<Shading> ShadingPolygons)
{
    public WeatherBuilder(float PanelElevation, float PanelAzimuth, float Latitude, float Longitude) : this(null, PanelElevation, PanelAzimuth, Latitude, Longitude, 1.0f, null, 1.0f, TimeSpan.FromHours(1), Enumerable.Empty<Shading>()) { }

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

    public WeatherBuilder WithTimeStep(TimeSpan timeSpan)
        => this with { TimeStep = timeSpan > TimeSpan.FromMinutes(1) ? timeSpan : throw new InvalidStateException($"Time step {timeSpan} must be greater than 1 minute") };

    public WeatherBuilder AddShadingToHorizon(float degreesAltitude) => AddShading(new Shading((0, -180), (degreesAltitude, -180), (degreesAltitude, 180), (0, 180)));

    public async Task<IGenerationProfile> BuildAsync()
    {
        if (DniProvider == null) throw new InvalidOperationException("Please supply a weather forecast provider e.g. call WithDniSource");

        // Weather forecast points in some coarse grained step.
        var forecast = await DniProvider.GetDniForecastAsync();
        if (forecast.Any() == false) throw new InvalidStateException("Weather forecast has no values");

        // Master clock that specifies the final spline, potentially in a finer grained step.
        IEnumerable<DateTimeOffset> Clock()
        {
            DateTimeOffset pointInTime = forecast.First().DateTime.ToClosestHour();
            while (pointInTime < forecast.Last().DateTime.ToClosestHour())
            {
                yield return pointInTime;
                pointInTime += TimeStep;
            }
        }

        // Function that can take a DateTimeOffset and a DniValue and return the actual GenerationValue.
        Func<DateTimeOffset, DniValue, GenerationValue> algorithm = (pointInTime, dni) =>
        {
            var sun = Sol.SunPositionRads(pointInTime, Latitude, Longitude);

            double irradiatedPower = Sol.DniToIrradiation(dni.DirectWatts, PanelAzimuth.ToRads(), PanelElevation.ToRads(), sun.Azimuth, sun.Altitude, dni.DiffuseWatts);
            irradiatedPower *= SunlightScalar;

            if (irradiatedPower > 0.0 && ShadingPolygons.Any(p => p.IsSunPositionShaded((sun.Altitude.ToDegrees(), sun.Azimuth.ToDegrees()))))
            {
                irradiatedPower = dni.DiffuseWatts ?? 0.0f;
            }

            float kw = IrradianceToPowerScalar * (float)irradiatedPower / 1000.0f;
            kw = Math.Min(kw, (AbsolutePeakWatts ?? int.MaxValue) / 1000.0f);

            return new GenerationValue(pointInTime, kw);
        };

        return new GenerationProfile(forecast, Clock(), algorithm);
    }
}