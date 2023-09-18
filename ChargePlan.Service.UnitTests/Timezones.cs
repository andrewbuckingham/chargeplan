using System.Diagnostics;

namespace ChargePlan.Service.UnitTests;

public class Timezones
{
    private static IPlant UnlimitedPlant() => new Hy36(1000.0f, 1000.0f, 1000.0f, 1000.0f, 1.0f, 100, 0);
    private static PowerAtAbsoluteTimes ZeroDemand() => new PowerAtAbsoluteTimes(
        Name: "Zero Demand",
        Values: new()
        {
            new (TimeOnly.MinValue, 0.0f),
            new (new(04,00), 0.0f),
            new (new(08,00), 0.0f),
            new (new(12,00), 0.0f),
            new (TimeOnly.MaxValue, 0.0f)
        }
    );
    private static PriceAtAbsoluteTimes UnitPrice() => new PriceAtAbsoluteTimes(
        Name: "Unit Price",
        Values: new()
        {
            new (TimeOnly.MinValue, 1.0M),
        }
    );

    [Fact]
    public void Bst_ShiftableDemandNow_StartsDemand()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithInitialBatteryEnergy(1000.0f)
            .WithExplicitStartDate(DateTime.Today.AddDays(1))
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(UnitPrice())
            .AddDemand(ZeroDemand())
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.0M, result.Evaluation.TotalCost);
    }

    // [Theory]
    // [InlineData(
    //     new float[] { 0.3f, 1.0f, 0.3f },
    //     new float[] { })]
    // public void Test1()
    // {
    //     var calc = new Calculator();

    //     DateTime datum = DateTime.Today;

    //     StorageProfile storageProfile = new() { CapacityKilowatts = 4.8f };

    //     DemandProfile demandProfile = new();
    // }

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