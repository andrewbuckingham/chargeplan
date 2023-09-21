using ChargePlan.Domain.Solver;

namespace ChargePlan.Service.UnitTests;

public class Efficiency
{
    private static IPlant UnlimitedPlant(float efficiencyPc) => new Hy36(1000.0f, 1000.0f, 1000.0f, 1000.0f, efficiencyPc / 100.0f, 100, 0);

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
    [InlineData(90, 0.1f * 4.0f)]
    [InlineData(50, 0.5f * 4.0f)]
    public void GridCharge_Discharge_ConsidersEfficiency(float efficiencyPc, float expectedCost)
    {
        var algorithm = new AlgorithmBuilder(UnlimitedPlant(efficiencyPc), Interpolations.Step())
            .WithPrecision(AlgorithmPrecision.Default with { TimeStep = TimeSpan.FromHours(1) })
            .WithGeneration(DateTime.Today.AddDays(1), new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }) // 4hrs generate, 4hrs not
            .ForDay(DateTime.Today.AddDays(1))
            .AddPricing(ConstantPrice(1.0M))
            .AddDemand(new PowerAtAbsoluteTimes(
                Name: "Constant Demand",
                Values: new()
                {
                    new (TimeOnly.MinValue, 0.0f), // 4hrs no demand, then 4hrs with demand.
                    new (new(04,00), 1.0f),
                    new (new(08,00), 0.0f),
                    new (new(09,00), 0.0f)
                }
            ))
            .Build();

        var result = algorithm.DecideStrategy();

        Assert.Equal(expectedCost, (float)result.Evaluation.TotalCost, 2);
    }
}