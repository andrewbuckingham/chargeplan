using System.Runtime.CompilerServices;
using ChargePlan.Domain.Splines;

namespace ChargePlan.Service.UnitTests;

public class UnitTest1
{
    private static IPlant Plant() => new Hy36(5.2f, 2.8f, 2.8f, 3.6f, 1.0f, 0.0f, 80, 5);

    [Theory]
    [InlineData(0, Math.PI / 2, 0, Math.PI / 2)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(Math.PI, Math.PI / 2, Math.PI, Math.PI / 2)]
    [InlineData(Math.PI, 0, Math.PI, 0)]
    public void SunAngle_ZeroAngleSubtendingPanelFrontFace_ProducesZero(double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation)
    {
        double irradiation = ChargePlan.Weather.Sol.DniToIrradiation(1.0, planeAzimuth, planeElevation, sunAzimith, sunElevation, null);
        Assert.Equal(0.0, irradiation, 8);
    }

    [Theory]
    [InlineData(0, 0, 0, Math.PI / 2)]
    [InlineData(0, Math.PI / 2, 0, 0)]
    [InlineData(Math.PI, 0, Math.PI, Math.PI / 2)]
    public void SunAngle_AtDirectNormalToPanel_ProducesMaximum(double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation)
    {
        double irradiation = ChargePlan.Weather.Sol.DniToIrradiation(1.0, planeAzimuth, planeElevation, sunAzimith, sunElevation, null);
        Assert.Equal(1.0, irradiation, 8);
    }

    [Fact]
    public void SunPosAzimuth_AtMidday_IsZeroForSouth()
    {
        var testDate = new DateTime(2023, 06, 23, 12, 00, 00, DateTimeKind.Utc);
        var pos = ChargePlan.Weather.Sol.SunPositionRads(testDate, 45.0f, 0.0f);
        Assert.Equal(0, pos.Azimuth, 1);
    }

    [Fact]
    public void SmallDemand_WithEnoughBattery_IsSatisfied()
    {
        var demand = new PowerAtAbsoluteTimes(
            Name: "SmallDemand_WithEnoughBattery_IsSatisfied",
            Values: new()
            {
                new (TimeOnly.MinValue, 1.0f),
                new (new(04,00), 1.0f),
                new (new(08,00), 1.0f),
                new (new(12,00), 1.0f),
                new (TimeOnly.MaxValue, 1.0f)
            }
        );

        var pricing = new PriceAtAbsoluteTimes(
            Name: "SmallDemand_WithEnoughBattery_IsSatisfied",
            Values: new()
            {
                new (TimeOnly.MinValue, 1.0M),
            }
        );

        var hugeBattery = new Hy36(1000.0f, 1000.0f, 1000.0f, 1000.0f, 1.0f, 0.0f, 100, 0);

        var algorithm = new AlgorithmBuilder(hugeBattery, Interpolations.Step())
            .WithInitialBatteryEnergy(1000.0f)
            .WithExplicitStartDate(DateTime.Today.AddDays(1))
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(pricing)
            .AddDemand(demand)
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(decimal.Zero, result.Evaluation.TotalCost);
    }

    [Fact]
    public void Generation_ProducesInterpolation_WithGoodMatch()
    {
        double[] powers = new double[]
        {
            0,
            0,
            0,
            0,
            0,
            0,
            3.3,
            14.3,
            49.5,
            103.2,
            155.9,
            179.7,
            246.7,
            334.2,
            405.2,
            421,
            400.6,
            404.5,
            338.2,
            218,
            92.7,
            6.3,
            0,
            0,
        };

        var values = powers.Select((f,i) => new GenerationValue(DateTime.Today.AddHours(i), (float)f));

        var generation = new GenerationProfile() { Values = values.ToList() };
        var spline = generation.AsSpline(new InterpolationFactory(Generation: InterpolationType.Step).InterpolateGeneration);

        var checks = Enumerable.Range(0,powers.Length * 4).Select(i => (
            Nearest: powers[i / 4],
            Interpolated: spline.Interpolate(DateTime.Today.AddMinutes(15 * i).AsTotalHours())));

        foreach (var check in checks)
        {
            Assert.InRange(check.Interpolated, check.Nearest * 0.99, check.Nearest * 1.01);
        }
    }
}