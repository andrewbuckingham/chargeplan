using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChargePlan.Domain.Splines;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Helpers;
using ChargePlan.Weather;

namespace ChargePlan.Service.UnitTests;

public class Weather
{
    private const double _whAtLatitude = 4.2;//3.4;

    private DateTimeOffset _solstice = new DateTimeOffset(DateTime.Today.Year, 06, 21, 12, 00, 00, TimeSpan.Zero);
    private IDirectNormalIrradianceProvider UnitDni() => new DummyDniProvider(_solstice);
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

    [Theory]
    [InlineData(9, 126, true)]
    public void OurPlant_Shading_IsCorrect(int alt, int az, bool shouldBeShaded)
    {
        var plant = MyPlantHack.UserPlantParameters();
        var shading = plant.ArrayShading;

        bool isShaded = shading.Single().IsSunPositionShaded((alt, az));
        Assert.Equal(shouldBeShaded, isShaded);
    }
}

file static class MyPlantHack
{
    private const string _myPlantHack = "{\"arraySpecification\":{\"arrayArea\":13.7,\"absolutePeakWatts\":2900,\"arrayElevationDegrees\":45,\"arrayAzimuthDegrees\":0,\"latDegrees\":54.5,\"longDegrees\":-1.55},\"weatherForecastSettings\":{\"sunlightScalar\":0.5358996,\"overcastScalar\":0.5358996},\"algorithmSettings\":{\"chargeRateLimitScalar\":1.2},\"arrayShading\":[{\"points\":[{\"item1\":90,\"item2\":0},{\"item1\":90,\"item2\":60},{\"item1\":90,\"item2\":70},{\"item1\":90,\"item2\":80},{\"item1\":30,\"item2\":90},{\"item1\":10,\"item2\":100},{\"item1\":15,\"item2\":110},{\"item1\":17,\"item2\":120},{\"item1\":5,\"item2\":130},{\"item1\":14,\"item2\":140},{\"item1\":18,\"item2\":150},{\"item1\":20,\"item2\":160},{\"item1\":20,\"item2\":170},{\"item1\":20,\"item2\":180},{\"item1\":20,\"item2\":190},{\"item1\":5,\"item2\":200},{\"item1\":14,\"item2\":210},{\"item1\":20,\"item2\":220},{\"item1\":15,\"item2\":230},{\"item1\":15,\"item2\":240},{\"item1\":19,\"item2\":250},{\"item1\":5,\"item2\":260},{\"item1\":27,\"item2\":270},{\"item1\":90,\"item2\":280},{\"item1\":90,\"item2\":290},{\"item1\":90,\"item2\":300},{\"item1\":90,\"item2\":310},{\"item1\":-90,\"item2\":310},{\"item1\":-90,\"item2\":0}]}],\"plantType\":\"Hy36\"}";
    public static UserPlantParameters UserPlantParameters(){
        Debug.WriteLine(_myPlantHack);
        string thing = Regex.Unescape(_myPlantHack);
        UserPlantParameters result = JsonSerializer.Deserialize<UserPlantParameters>(thing,
            new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            })!;
        return result;
    }
}
