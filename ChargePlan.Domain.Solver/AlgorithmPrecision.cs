namespace ChargePlan.Domain.Solver;

/// <summary>
/// 
/// </summary>
/// <param name="TimeStep">Granularity of the simulation</param>
/// <param name="IterateInPercents">What levels of charge rates to try (charging and discharging). Null to disable.</param>
/// <param name="ShiftBy">How much to shift the shiftable demands by</param>
public record AlgorithmPrecision(TimeSpan TimeStep, int? IterateInPercents, TimeSpan ShiftBy)
{
    public static AlgorithmPrecision Default = new(TimeSpan.FromMinutes(5), 15, TimeSpan.FromMinutes(15));
}