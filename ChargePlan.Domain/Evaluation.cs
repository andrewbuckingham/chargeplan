namespace ChargePlan.Domain;

/// <summary>
/// The outcome from a Calculator having iterated the energy in/out and battery position.
/// </summary>
public record Evaluation(float? ChargeRateLimit, float? DischargeRateLimit, decimal TotalCost, List<IntegrationStep> DebugResults, List<OverchargePeriod> OverchargePeriods, List<UnderchargePeriod> UnderchargePeriods)
{
    public override string ToString() => $"Total: £{TotalCost}, Charge rate limit: {ChargeRateLimit?.ToString() ?? "Any "}kW, Disharge rate limit: {DischargeRateLimit?.ToString() ?? "Any "}kW";
}

/// <summary>
/// Amount of overcharge energy, up until a period of undercharge.
/// </summary>
public record OverchargePeriod(
    DateTimeOffset From,
    DateTimeOffset To,
    float OverchargeEnergy
);

public record UnderchargePeriod(
    DateTimeOffset From,
    DateTimeOffset To,
    float UnderchargeEnergy
);