using ChargePlan.Domain.Solver;

namespace ChargePlan.Service.UnitTests;

public class Overcharge
{
    private static IPlant LimitedThroughputPlant(float throughput) => new Hy36(1000.0f, 1000.0f, 1000.0f, throughput, 1.0f, 0.0f, 100, 0);
    private static IPlant LimitedCapacityBattery(float capacity) => new Hy36(capacity, 1000.0f, 1000.0f, 1000.0f, 1.0f, 0.0f, 100, 0);
    private static IPlant LimitedDischargeBattery(float throughput) => new Hy36(1000.0f, 1000.0f, throughput, 1000.0f, 1.0f, 0.0f, 100, 0);
    private static PowerAtAbsoluteTimes ConstantDemand(float kw) => new PowerAtAbsoluteTimes(
        Name: "Constant Demand",
        Values: new()
        {
            new (TimeOnly.MinValue, kw),
            new (new(04,00), kw),
            new (new(08,00), kw),
            new (new(12,00), kw),
            new (TimeOnly.MaxValue, kw)
        }
    );
    private static PriceAtAbsoluteTimes ConstantPrice(decimal perHour) => new PriceAtAbsoluteTimes(
        Name: $"{perHour} per hour",
        Values: new()
        {
            new (TimeOnly.MinValue, perHour),
        }
    );

    private static IntegrationStep IntegrationStep(int number, float cumulativeOvercharge) =>
        new(DateTime.Today.AddDays(1).AddMinutes(number), 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, cumulativeOvercharge, new(0.0f), new IntegrationStepDemandEnergy[] {});

    [Fact]
    public void IntegrationSteps_WhenOvercharge_IsDetected()
    {
        var steps = new IntegrationStep[] {
            IntegrationStep(1, 0.0f),
            IntegrationStep(2, 1.0f),
            IntegrationStep(3, 2.0f),
            IntegrationStep(4, 3.0f),
            IntegrationStep(5, 3.0f),
            IntegrationStep(6, 3.0f),
            IntegrationStep(7, 3.0f),
            IntegrationStep(8, 3.0f)            
        };
        var results = steps.CalculateOverchargePeriods(TimeSpan.FromMinutes(1));

        Assert.True(results.Overcharge.Count == 1);
    }

    [Fact]
    public void OverchargeNow_DueToCapacity_IsDetected()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(1.0f), Interpolations.Step())
            // .WithPrecision(AlgorithmPrecision.Default with { IterateInPercents = 100 })
            .WithInitialBatteryEnergy(1.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1, result.Evaluation.OverchargePeriods.Count);
        Assert.Equal(TimeSpan.Zero, result.Evaluation.OverchargePeriods.Single().From.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(4), result.Evaluation.OverchargePeriods.Single().To.TimeOfDay);
    }

    [Fact]
    public void OverchargeNow_DueToImminentCapacity_IsDetected()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(2.0f), Interpolations.Step())
            .WithInitialBatteryEnergy(0.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1, result.Evaluation.OverchargePeriods.Count);
        Assert.Equal(TimeSpan.Zero, result.Evaluation.OverchargePeriods.Single().From.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(4), result.Evaluation.OverchargePeriods.Single().To.TimeOfDay);
    }

    [Fact]
    public void OverchargeLater_DueToCapacity_IsDetected()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(2.0f), Interpolations.Step())
            .WithInitialBatteryEnergy(0.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1, result.Evaluation.OverchargePeriods.Count);
        Assert.Equal(TimeSpan.FromHours(1), result.Evaluation.OverchargePeriods.Single().From.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(5), result.Evaluation.OverchargePeriods.Single().To.TimeOfDay);
    }

    [Fact]
    public void OverchargeNow_WithBatteryNowBufferingSolarLater_IsDetected()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(2.0f), Interpolations.Step())
            .WithInitialBatteryEnergy(1.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1, result.Evaluation.OverchargePeriods.Count);
        Assert.Equal(TimeSpan.Zero, result.Evaluation.OverchargePeriods.Single().From.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(5), result.Evaluation.OverchargePeriods.Single().To.TimeOfDay);
    }
}