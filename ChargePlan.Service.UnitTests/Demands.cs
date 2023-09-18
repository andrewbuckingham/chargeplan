namespace ChargePlan.Service.UnitTests;

public class Demands
{
    private static IPlant UnlimitedPlant() => new Hy36(1000.0f, 1000.0f, 1000.0f, 1000.0f, 1.0f, 100, 0);
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
    public void BasicDemand_GridOnly_CorrectCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(1.0M * 24.0M - 1.0M * (decimal)Calculator.TimeStep.TotalHours, result.Evaluation.TotalCost, 2);
    }

    [Fact]
    public void BasicDemand_PerfectPvOnly_ZeroCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 1.0f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(decimal.Zero, result.Evaluation.TotalCost);
    }

    [Fact]
    public void BasicDemand_HalfPvHalfGrid_CorrectCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 0.5f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(0.5M * 24.0M - 0.5M * (decimal)Calculator.TimeStep.TotalHours, result.Evaluation.TotalCost, 1);
    }

    [Fact]
    public void BasicDemand_BatteryOnly_ZeroCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithInitialBatteryEnergy(24.0f)
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(decimal.Zero, result.Evaluation.TotalCost);
    }

    [Fact]
    public void BasicDemand_HalfBatteryHalfPv_ZeroCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithInitialBatteryEnergy(12.0f)
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 0.5f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(decimal.Zero, result.Evaluation.TotalCost);
    }

    [Fact]
    public void BasicDemand_QtrBatteryQtrPv_HalfCost()
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(), Interpolations.Step())
            .WithInitialBatteryEnergy(6.0f)
            .WithGeneration(DateTime.Today.AddDays(1), Enumerable.Range(0, 24).Select(f => 0.25f).ToArray())
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(ConstantDemand(1.0f))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(12.0M - 0.25M * (decimal)Calculator.TimeStep.TotalHours, result.Evaluation.TotalCost, 0);
    }
}