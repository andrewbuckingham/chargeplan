namespace ChargePlan.Domain.Solver;

/// <summary>
/// 
/// </summary>
/// <param name="TimeStep">Granularity of the simulation</param>
/// <param name="IterateInPercents">What levels of charge rates to try (charging and discharging). Null to disable.</param>
/// <param name="ShiftBy">How much to shift the shiftable demands by</param>
public record AlgorithmPrecision(TimeSpan TimeStep, int? IterateInPercents, TimeSpan ShiftBy, DynamicChargeWindowCalculations AutoChargeWindow)
{
    public static readonly AlgorithmPrecision Default = new(
        TimeStep: TimeSpan.FromMinutes(10),
        IterateInPercents: 10,
        ShiftBy: TimeSpan.FromMinutes(30),
        AutoChargeWindow: DynamicChargeWindowCalculations.Default
    );
}


public record DynamicChargeWindowCalculations(int MaxPricingIterations, int MaxRateIterations)
{
    public static readonly DynamicChargeWindowCalculations Default = new(
        MaxPricingIterations: 4,
        MaxRateIterations: 8
    );
}