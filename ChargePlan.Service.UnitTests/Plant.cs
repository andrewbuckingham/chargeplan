namespace ChargePlan.Service.UnitTests;

public class Plant
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
    private static PowerAtAbsoluteTimes ConstantDemand(float kw, TimeOnly startingFrom, int hours) => new PowerAtAbsoluteTimes(
        Name: "Some Demand",
        Values: Enumerable.Range(0, hours * 60).Select(f => ((startingFrom.AddMinutes(f), kw))).ToList()
    );
    private static PriceAtAbsoluteTimes ConstantPrice(decimal perHour) => new PriceAtAbsoluteTimes(
        Name: $"{perHour} per hour",
        Values: new()
        {
            new (TimeOnly.MinValue, perHour),
        }
    );

    [Fact]
    public void HalfThroughput_UnlimitedPv_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedThroughputPlant(0.5f))
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 1.0f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 24.0M - 0.25M * 0.5M, result.Evaluation.TotalCost, 1);
    }

    [Fact]
    public void HalfDischarge_NoPv_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedDischargeBattery(0.5f))
            .WithInitialBatteryEnergy(1000.0f)
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 24.0M - 0.25M * 0.5M, result.Evaluation.TotalCost, 1);
    }

    [Fact]
    public void BatterySaturated_HalfCapacity_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(6.0f))
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 12).Select(f => 1.0f).Append(0.0f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(0.0f, TimeOnly.MinValue, 12)) // First 12hrs no demand while battery charging on PV
            .AddDemand(ConstantDemand(1.0f, TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), 12)) // PV stops and demand starts, remaining 12hrs
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 12.0M - 0.25M * 1.0M, result.Evaluation.TotalCost);
    }
}