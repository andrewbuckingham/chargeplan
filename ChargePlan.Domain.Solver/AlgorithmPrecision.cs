namespace ChargePlan.Domain.Solver;

public record AlgorithmPrecision(TimeSpan TimeStep, int IterateInPercents)
{
    public static AlgorithmPrecision Default = new(TimeSpan.FromMinutes(5), 20);
}