namespace ChargePlan.Service.UnitTests;

public class UnitTest1
{
    private static IPlant Plant() => new Hy36(5.2f, 2.8f, 2.8f, 3.6f, 80, 5);

    [Theory]
    [InlineData(0, Math.PI / 2, 0, Math.PI / 2)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(Math.PI, Math.PI / 2, Math.PI, Math.PI / 2)]
    [InlineData(Math.PI, 0, Math.PI, 0)]
    public void SunAngle_ZeroAngleSubtendingPanelFrontFace_ProducesZero(double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation)
    {
        double irradiation = ChargePlan.Weather.Sol.DniToIrradiation(1.0, planeAzimuth, planeElevation, sunAzimith, sunElevation);
        Assert.Equal(0.0, irradiation, 8);
    }

    [Theory]
    [InlineData(0, 0, 0, Math.PI / 2)]
    [InlineData(0, Math.PI / 2, 0, 0)]
    [InlineData(Math.PI, 0, Math.PI, Math.PI / 2)]
    [InlineData(Math.PI, 0, Math.PI, Math.PI / 2)]
    public void SunAngle_AtDirectNormalToPanel_ProducesMaximum(double planeAzimuth, double planeElevation, double sunAzimith, double sunElevation)
    {
        double irradiation = ChargePlan.Weather.Sol.DniToIrradiation(1.0, planeAzimuth, planeElevation, sunAzimith, sunElevation);
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

        var hugeBattery = new Hy36(1000.0f, 1000.0f, 1000.0f, 1000.0f, 100, 0);

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

    // [Fact]
    // public void ShiftableDemand_ProducesInterpolation()
    // {
    //     var dehumidifiers = new ShiftableDemandValue[]
    //     {
    //         new (TimeSpan.FromHours(0.0), 1.0f),
    //         new (TimeSpan.FromHours(1.0), 0.0f)
    //     };

    //     var datum = DateTime.Today;

    //     var demand = new ShiftableDemand() { Values = dehumidifiers.ToList() }
    //         .AsDemandProfile(datum.AddHours(1))
    //         .AsSpline(StepInterpolation.Interpolate);

    //     var beforePeriod = demand.Interpolate(datum.AddHours(0.0).AsTotalHours());
    //     var duringPeriod = demand.Interpolate(datum.AddHours(1.9).AsTotalHours());
    //     var afterPeriod = demand.Interpolate(datum.AddHours(4).AsTotalHours());

    //     Assert.Equal(0.0, beforePeriod, 1);
    //     Assert.Equal(1.0, duringPeriod, 1);
    //     Assert.Equal(0.0, afterPeriod, 1);
    // }
}