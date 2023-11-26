namespace ChargePlan.Domain;

public record IntegrationStep(
    DateTimeOffset DateTime,
    float BatteryEnergy,
    float DemandEnergy,
    float GenerationEnergy,
    float ChargeEnergy,
    float ExportEnergy,
    float CumulativeCost,
    float CumulativeUndercharge,
    float CumulativeOvercharge,
    PowerValues PowerValues,
    IntegrationStepDemandEnergy[] DemandEnergies
);

public record IntegrationStepDemandEnergy(string Name, string Type, float Energy, float Power);

public record PowerValues(float Generation);

public static class IntegrationStepEnumerableExtensions
{
    private enum Direction { Indeterminate = 0, Undercharge, Overcharge };
    private record Accumulator(float Amount, Direction Direction, DateTimeOffset Since);

    public static (List<OverchargePeriod> Overcharge, List<UnderchargePeriod> Undercharge) CalculateOverchargePeriods(this IEnumerable<IntegrationStep> integrationSteps, TimeSpan timeStep)
    {
        List<OverchargePeriod> overchargePeriods = new();
        List<UnderchargePeriod> underchargePeriods = new();
        Accumulator accumulator = new(
            0.0f,
            Direction.Indeterminate,
            (integrationSteps.FirstOrDefault()?.DateTime ?? throw new InvalidOperationException("No elements - does timestep exceed total time?")) - timeStep);

        var sourceData = integrationSteps
            .Zip(integrationSteps.Skip(1).Append(null))
            .Select(pair => (
                HasUnderchargeOccurred: pair.Second?.CumulativeUndercharge > pair.First.CumulativeUndercharge,
                HasOverchargeOccurred: pair.Second?.CumulativeOvercharge > pair.First.CumulativeOvercharge,
                First: pair.First,
                Second: pair.Second
            ));

        foreach (var pair in sourceData)
        {
            if (pair.HasOverchargeOccurred) accumulator = accumulator with
            {
                Direction = Direction.Overcharge,
                Amount = accumulator.Amount + ((pair.Second?.CumulativeOvercharge ?? pair.First.CumulativeOvercharge) - pair.First.CumulativeOvercharge)
            };
            else if (pair.HasUnderchargeOccurred) accumulator = accumulator with
            {
                Direction = Direction.Undercharge,
                Amount = accumulator.Amount + ((pair.Second?.CumulativeUndercharge ?? pair.First.CumulativeUndercharge) - pair.First.CumulativeUndercharge)
            };
            else if (accumulator.Direction == Direction.Overcharge)
            {
                // No longer overcharge, but we were previously in a period of such. Add and clear down.
                overchargePeriods.Add(new OverchargePeriod(accumulator.Since, pair.First.DateTime, accumulator.Amount));
                accumulator = new(0.0f, Direction.Indeterminate, pair.First.DateTime);
            }
            else if (accumulator.Direction == Direction.Undercharge)
            {
                // No longer overcharge, but we were previously in a period of such. Add and clear down.
                underchargePeriods.Add(new UnderchargePeriod(accumulator.Since, pair.First.DateTime, accumulator.Amount));
                accumulator = new(0.0f, Direction.Indeterminate, pair.First.DateTime);
            }
            else
            {
                accumulator = accumulator with { Direction = Direction.Indeterminate };
            }
        }

        return (overchargePeriods, underchargePeriods);
    }
}