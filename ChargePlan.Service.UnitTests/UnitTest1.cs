namespace ChargePlan.Service.UnitTests;

public class UnitTest1
{
    private static IPlant Plant() => new Hy36(5.2f, 2.8f, 2.8f, 3.6f, 80, 5);

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