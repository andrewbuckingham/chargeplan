using ChargePlan.Domain.Splines;
using ChargePlan.Weather;

namespace ChargePlan.Service.UnitTests;

public class Weather
{
    private const double _whAtLatitude = 4.2;

    private DateTime _solstice = new DateTime(DateTime.Today.Year, 06, 21, 12, 00, 00);
    private IDirectNormalIrradianceProvider UnitDni() => new DummyDni(_solstice);

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

        var spline = weather.AsSpline(new InterpolationFactory().InterpolateGeneration);
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

        var spline = weather.AsSpline(new InterpolationFactory().InterpolateGeneration);
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

file record DummyDni(DateTime day) : IDirectNormalIrradianceProvider
{
    public async Task<IEnumerable<(DateTime DateTime, float DirectWatts, float? DiffuseWatts)>> GetDniForecastAsync()
    {
        IEnumerable<(DateTime DateTime, float DirectWatts, float? DiffuseWatts)> Iterate()
        {
            DateTime dt = day.Date;
            DateTime end = day.Date.AddDays(1);

            while (dt < end)
            {
                yield return (dt, 1000.0f, null);
                dt += TimeSpan.FromMinutes(1);
                //dt += TimeSpan.FromHours(4);
            }
        };

        var result = Iterate().ToArray();

        return result;
    }
}