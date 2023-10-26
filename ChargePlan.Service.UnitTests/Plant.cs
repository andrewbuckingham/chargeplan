using System.Text;

namespace ChargePlan.Service.UnitTests;

public class Plant
{
    private static IPlant LimitedThroughputPlant(float throughput, float chargingEfficiency = 1.0f) => new Hy36(1000.0f, 1000.0f, 1000.0f, throughput, chargingEfficiency, 100, 0);
    private static IPlant LimitedCapacityBattery(float capacity, float chargingEfficiency = 1.0f) => new Hy36(capacity, 1000.0f, 1000.0f, 1000.0f, chargingEfficiency, 100, 0);
    private static IPlant LimitedDischargeBattery(float throughput, float chargingEfficiency = 1.0f) => new Hy36(1000.0f, 1000.0f, throughput, 1000.0f, chargingEfficiency, 100, 0);
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
    private static PriceAtAbsoluteTimes OctopusGoPrice(decimal perHourOvernight, decimal perHourDaytime) => new PriceAtAbsoluteTimes(
        Name: $"{perHourOvernight}/{perHourDaytime} per hour",
        Values: new()
        {
            new (TimeOnly.MinValue, perHourDaytime),
            new (new TimeOnly(0, 30, 0), perHourOvernight),
            new (new TimeOnly(4, 30, 0), perHourDaytime),
            new (TimeOnly.MaxValue, perHourDaytime)
        }
    );

    [Fact]
    public void HalfThroughput_UnlimitedPv_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedThroughputPlant(0.5f), Interpolations.Step())
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 1.0f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 24.0M - 0.5M * (decimal)algorithm.AlgorithmPrecision.TimeStep.TotalHours, result.Evaluation.TotalCost, 1);
    }

    [Fact]
    public void HalfDischarge_NoPv_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedDischargeBattery(0.5f), Interpolations.Step())
            .WithInitialBatteryEnergy(1000.0f)
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 24.0M - 0.5M * (decimal)algorithm.AlgorithmPrecision.TimeStep.TotalHours, result.Evaluation.TotalCost, 1);
    }

    [Fact]
    public void BatterySaturated_HalfCapacity_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(LimitedCapacityBattery(6.0f), Interpolations.Step())
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 12).Select(f => 1.0f).Append(0.0f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(0.0f, TimeOnly.MinValue, 12)) // First 12hrs no demand while battery charging on PV
            .AddDemand(ConstantDemand(1.0f, TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), 12)) // PV stops and demand starts, remaining 12hrs
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 12.0M - 1.0M * (decimal)algorithm.AlgorithmPrecision.TimeStep.TotalHours, result.Evaluation.TotalCost, 2);
    }

    [Fact]
    public void LimitedPlant_WhenGridChargingWithPoorSolar_HasUnlimitedThroughputAndUsesOvernightCharge()
    {
        var hugeShiftableDemand = PowerAtRelativeTimes.Empty() with {Values = new (TimeSpan RelativeTime, float Power)[]
        {
            (TimeSpan.Zero, 1.0f), // 1 kW for 2 hrs. Should exceed plant throughput when have multiple of them.
            (new TimeSpan(2,0,0), 0.0f)
        }.ToList()};

        decimal perHourOvernight = 0.1M;
        decimal perHourDaytime = 1.0M;
        
        var algorithm = new AlgorithmBuilder(LimitedThroughputPlant(1.0f), Interpolations.Step())
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.1f, 0.1f, 0.1f, 0.1f }) // Tiny bit of daytime generation, sohuld be ignored
            .AddShiftableDemandAnyDay(hugeShiftableDemand with { Name = "Demand 1" }, priority: ShiftableDemandPriority.Essential)
            .AddShiftableDemandAnyDay(hugeShiftableDemand with { Name = "Demand 2" }, priority: ShiftableDemandPriority.Essential)
            .AddShiftableDemandAnyDay(hugeShiftableDemand with { Name = "Demand 3" }, priority: ShiftableDemandPriority.Medium)
            .AddShiftableDemandAnyDay(hugeShiftableDemand with { Name = "Demand 4" }, priority: ShiftableDemandPriority.Low)
            .ForDay(DateTime.Today.AddDays(1))
            .AddDemand(ConstantDemand(0.11f))
            .AddPricing(OctopusGoPrice(perHourOvernight, perHourDaytime))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.True(result.ShiftableDemands.All(f => f.StartAt == DateTime.Today.AddDays(1).AddMinutes(30))); // Everything starts as soon as the cheap overnight period begins.
    }
}