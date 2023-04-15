/// <summary>
/// The outcome from a Calculator having iterated the energy in/out and battery position.
/// </summary>
public record Evaluation(float? ChargeRateLimit, float UnderchargeEnergy, float OverchargeEnergy, decimal TotalCost, List<IntegrationStep> DebugResults);