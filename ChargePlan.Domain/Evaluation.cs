namespace ChargePlan.Domain;

/// <summary>
/// The outcome from a Calculator having iterated the energy in/out and battery position.
/// </summary>
public record Evaluation(float? ChargeRateLimit, float UnderchargeEnergy, float OverchargeEnergy, decimal TotalCost, List<IntegrationStep> DebugResults)
{
    public float OverchargeEnergyToday
    {
        get
        {
            var todaysResults = DebugResults.Where(f => f.DateTime.Date == DateTime.Today).OrderBy(f => f.DateTime);
            var last = todaysResults.LastOrDefault();
            var first = todaysResults.FirstOrDefault();
            if (last == null || first == null) return 0.0f;

            return last.CumulativeOvercharge - first.CumulativeOvercharge;
        }
    }

    public override string ToString() => $"Total: £{TotalCost}, Charge rate limit: {ChargeRateLimit?.ToString() ?? "Any "}kW";
}