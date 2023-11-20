using ChargePlan.Domain.Solver;

namespace ChargePlan.Service.UnitTests;

public class Efficiency
{
    private static IPlant UnlimitedPlant(float efficiencyPc, float i2r = 0.0f) => new Hy36(1000.0f, 100.0f, 100.0f, 100.0f, efficiencyPc / 100.0f, i2r, 100, 0);
    private static IPlant LimitedPlant(float maxBatteryThroughput, float efficiencyPc, float i2r = 0.0f) => new Hy36(1000.0f, maxBatteryThroughput, maxBatteryThroughput, 100.0f, efficiencyPc / 100.0f, i2r, 100, 0);

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

    [Theory]
    [InlineData(100, 0.0f)]
    [InlineData(90, 0.1f * 1.0f)]
    [InlineData(50, 0.5f * 1.0f)]
    public void GridCharge_Discharge_ConsidersEfficiency(float efficiencyPc, float expectedCost)
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(efficiencyPc), Interpolations.Step())
            .WithPrecision(f => f with
            {
                TimeStep = TimeSpan.FromHours(1),
                IterateInPercents = null
            })
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 1.0f, 0.0f }) // 4hrs generate, 4hrs not
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(new PowerAtAbsoluteTimes(
                Name: "Constant Demand",
                Values: new()
                {
                    new (TimeOnly.MinValue, 0.0f), // 4hrs no demand, then 4hrs with demand.
                    new (new(01,00), 1.0f),
                    new (new(02,00), 0.0f)
                }
            ))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(expectedCost, (float)result.Evaluation.TotalCost, 2);
    }

    [Fact]
    public void DischargingUnlimitedPlant_WhenEnergyShortage_ScalesForSlowRampdown()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(efficiencyPc: 100, i2r: 0.0f), Interpolations.Step())
            .WithInitialBatteryEnergy(8.0f)
            .WithPrecision(AlgorithmPrecision.Default with { IterateInPercents = 1, TimeStep = TimeSpan.FromMinutes(60) })
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddShiftableDemand(new PowerAtRelativeTimes(
                Name: "Shiftable Demand",
                Values: new()
                {
                    new (TimeSpan.Zero, 1.0f),
                    new (TimeSpan.FromMinutes(15), 0.0f)
                }
            ))
            .AddDemand(new PowerAtAbsoluteTimes(
                Name: "Constant Demand",
                Values: new()
                {
                    new (TimeOnly.MinValue, 1.0f), // 4hrs no demand, then 4hrs with demand.
                    new (new(08,00), 1.0f),
                }
            ))
            .Build();

        var result = algorithm.DecideStrategy();

        // Battery has 8kWh, and needs to give a total of 8kWh over the 8hr period, so should
        // discharge at 1kW to give a smooth average.
        Assert.Equal(1.0f, result.Evaluation.DischargeRateLimit);
    }

    [Fact]
    public void DischargingLimitedPlant_WithNoCharge_MinimisesDischarge()
    {
        var algorithm = new AlgorithmBuilder(LimitedPlant(maxBatteryThroughput: 4.0f, efficiencyPc: 100, i2r: 0.0f), Interpolations.Step())
            .WithInitialBatteryEnergy(8.0f)
            .WithPrecision(AlgorithmPrecision.Default with { IterateInPercents = 10 })
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddShiftableDemand(new PowerAtRelativeTimes(
                Name: "Shiftable Demand Any",
                StartWheneverCheaperThan: 0.1M,
                Values: new()
                {
                    new (TimeSpan.Zero, 2.0f),
                    new (TimeSpan.FromHours(1), 0.0f)
                }
            ))
            .AddShiftableDemand(new PowerAtRelativeTimes(
                Name: "Shiftable Demand When Empty",
                Earliest: new TimeOnly(04, 00),
                Latest: new TimeOnly(08, 00),
                Values: new()
                {
                    new (TimeSpan.Zero, 2.0f),
                    new (TimeSpan.FromHours(1), 0.0f)
                }
            ))
            .AddDemand(new PowerAtAbsoluteTimes(
                Name: "Constant Demand",
                Values: new()
                {
                    new (TimeOnly.MinValue, 4.0f), // 4hrs no demand, then 4hrs with demand.
                    new (new(08,00), 0.0f),
                }
            ))
            .AddChargeWindow(new PowerAtAbsoluteTimes(
                Name: "Overnight Charge",
                Values: new()
                {
                    new (new(01,00), 1.0f),
                    new (new(02,00), 0.0f),
                }
            ))
            .Build();

        var result = algorithm.DecideStrategy();

        // Battery has 8kWh, and needs to give a total of 8kWh over the 8hr period, so should
        // discharge at 1kW to give a smooth average.
        Assert.Equal(1.0f, result.Evaluation.DischargeRateLimit);
    }

    // [Theory]
    // [InlineData(1.0f, 0.0f, 0.0f)]
    // [InlineData(0.5f, 0.0f, 0.0f)]

    // [InlineData(1.0f, 0.5f, 0.5f)]
    // [InlineData(0.5f, 0.5f, 0.25f)]

    // [InlineData(1.0f, 1.0f, 1.0f)]
    // [InlineData(0.5f, 1.0f, 0.5f)]
    public void I2RLosses_Discharge_ConsidersEfficiency(float demand, float i2rScalar, float shortfallExpected)
    {
        var algorithm = new AlgorithmBuilder(LimitedPlant(maxBatteryThroughput: 1.0f, efficiencyPc: 100, i2r: i2rScalar), Interpolations.Step())
            .WithInitialBatteryEnergy(demand * 10) // Power * Hours
            .WithPrecision(AlgorithmPrecision.Default with
            {
                TimeStep = TimeSpan.FromHours(1),
                IterateInPercents = null
            })
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f })
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(new PowerAtAbsoluteTimes(
                Name: "Constant Demand",
                Values: new()
                {
                    new (TimeOnly.MinValue, demand), // 4hrs no demand, then 4hrs with demand.
                    new (new(10,00), demand),
                }
            ))
            .Build();

        var result = algorithm.DecideStrategy();

        // Battery has 8kWh, and needs to give a total of 8kWh over the 8hr period, so should
        // discharge at 1kW to give a smooth average.
        Assert.Equal(shortfallExpected, result.Evaluation.UnderchargePeriods.SingleOrDefault()?.UnderchargeEnergy ?? 0);
    }
}