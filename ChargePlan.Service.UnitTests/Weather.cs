using ChargePlan.Domain.Splines;
using ChargePlan.Weather;

namespace ChargePlan.Service.UnitTests;

public class Weather
{
    private const double _whAtLatitude = 4.2;//3.4;

    private DateTimeOffset _solstice = new DateTimeOffset(DateTime.Today.Year, 06, 21, 12, 00, 00, TimeSpan.Zero);
    private IDirectNormalIrradianceProvider UnitDni() => new DummyDni(_solstice);
    private InterpolationFactory InterpolationFactory() => new InterpolationFactory(Generation: InterpolationType.Step);

    // [Fact]
    // public async Task WithUnitIrradiance_ProducesCorrectTrig()
    // {
    //     var weather = await new WeatherBuilder(0.0f, 0.0f, 54.0f, 0.0f)
    //         .WithArrayArea(1.0f, 100.0f)
    //         .WithDniSource(new DummyDni(_solstice))
    //         .BuildAsync();

    //     var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
    //         .WithGeneration(weather)
    //         .ForDay(_solstice)
    //         .AddPricing(ConstantPrice(0.0M))
    //         .AddDemand(ConstantDemand(0.0f))
    //         .Build();

    //     var result = algorithm.DecideStrategy();

    //     var trial = result.Evaluation.DebugResults.Where(f=>f.DateTime.TimeOfDay == new TimeSpan(16,30,00)).Single();
    //     // Assert.Equal(trial.PowerValues.Generation, 0.01, 2);

    //     var things = (await new DummyDni(_solstice).GetDniForecastAsync()).Select(f =>
    //         {
    //             var sun = Sol.SunPositionRads(f.DateTime, 54, 0);

    //             double irradiatedPower = Sol.DniToIrradiation(f.DirectWatts, 0.0f.ToRads(), 45.0f.ToRads(), sun.Azimuth, sun.Altitude, f.DiffuseWatts);
    //             return new GenerationValue(f.DateTime.ToLocalTime(), (float)irradiatedPower);
    //         }).ToArray();

    //     Assert.True(things.Where(f=>f.DateTime.TimeOfDay == new TimeSpan(17,30,00)).First().Power == 1.0f);
    // }

    [Theory]
    [InlineData(1.0f, 100.0f, _whAtLatitude)]
    [InlineData(2.0f, 100.0f, 2.0 * _whAtLatitude)]
    [InlineData(2.0f, 50.0f, _whAtLatitude)]
    public async Task WithoutShading_AllUnshaded(float arrayArea, float efficiencyPc, double expectedWh)
    {
        var weather = await new WeatherBuilder(0.0f, 0.0f, 54.0f, 0.0f)
            .WithArrayArea(arrayArea, efficiencyPc)
            .WithDniSource(UnitDni())
            .BuildAsync();

        var spline = weather.AsSpline(InterpolationFactory().InterpolateGeneration);
        double wattHours = spline.Integrate(_solstice.Date.AsTotalHours(), _solstice.Date.AddDays(1).AsTotalHours());
        Assert.Equal(expectedWh, wattHours, 2);
    }

    [Theory]
    [InlineData(1.0f, 100.0f, 0.5 * _whAtLatitude)]
    [InlineData(2.0f, 100.0f, 0.5 * 2.0 * _whAtLatitude)]
    [InlineData(2.0f, 50.0f, 0.5 * _whAtLatitude)]
    public async Task WithShadingFirstHalfOfDay_HalfTotalEnergy(float arrayArea, float efficiencyPc, double expectedWh)
    {
        var weather = await new WeatherBuilder(0.0f, 0.0f, 54.0f, 0.0f)
            .WithArrayArea(arrayArea, efficiencyPc)
            .WithDniSource(UnitDni())
            .AddShading(new Shading(new[] {
                (0.0f, -180.0f),
                (90.0f, -180.0f),
                (90.0f, 0.0f),
                (0.0f, 0.0f),
            }))
            .BuildAsync();

        var spline = weather.AsSpline(InterpolationFactory().InterpolateGeneration);
        double wattHours = spline.Integrate(_solstice.Date.AsTotalHours(), _solstice.Date.AddDays(1).AsTotalHours());
        Assert.Equal(expectedWh, wattHours, 1);
    }

    [Fact]
    public async Task WithShadingToZenith_NoEnergy()
    {
        var weather = await new WeatherBuilder(0.0f, 0.0f, 54.0f, 0.0f)
            .WithArrayArea(1.0f, 100.0f)
            .WithDniSource(UnitDni())
            .AddShadingToHorizon(70.0f)
            .BuildAsync();

        var spline = weather.AsSpline(new InterpolationFactory().InterpolateGeneration);
        double wattHours = spline.Integrate(_solstice.Date.AsTotalHours(), _solstice.Date.AddDays(1).AsTotalHours());
        Assert.Equal(0, wattHours);
    }
}

file record DummyDni(DateTimeOffset day) : IDirectNormalIrradianceProvider
{
    public async Task<IEnumerable<DniValue>> GetDniForecastAsync()
    {
        IEnumerable<DniValue> Iterate()
        {
            DateTimeOffset dt = day.Date;
            DateTimeOffset end = day.Date.AddDays(4);

            while (dt < end)
            {
                yield return new DniValue(dt.LocalDateTime, 1000.0f, null, 0);
                dt += TimeSpan.FromHours(0.5);
            }
        };

        var result = Iterate().ToArray();

        return result;
    }
}