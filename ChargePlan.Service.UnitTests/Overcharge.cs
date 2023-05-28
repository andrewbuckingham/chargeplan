namespace ChargePlan.Service.UnitTests;

public class Overcharge
{
    private static IPlant LimitedThroughputPlant(float throughput) => new Hy36(1000.0f, 1000.0f, 1000.0f, throughput, 100, 0);
    private static IPlant LimitedCapacityBattery(float capacity) => new Hy36(capacity, 1000.0f, 1000.0f, 1000.0f, 100, 0);
    private static IPlant LimitedDischargeBattery(float throughput) => new Hy36(1000.0f, 1000.0f, throughput, 1000.0f, 100, 0);
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

    [Fact]
    public void OverchargeNow_DueToCapacity_IsDetected()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(1.0f))
            .WithInitialBatteryEnergy(1.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(0.0M))
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
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(2.0f))
            .WithInitialBatteryEnergy(0.0f)
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 2.0f, 2.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(0.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1, result.Evaluation.OverchargePeriods.Count);
        Assert.Equal(TimeSpan.Zero, result.Evaluation.OverchargePeriods.Single().From.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(4), result.Evaluation.OverchargePeriods.Single().To.TimeOfDay);
    }
}