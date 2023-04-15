namespace ChargePlan.Service;

public record Decision(float? RecommendedChargeRateLimit, float UnderchargeEnergy, float OverchargeEnergy, decimal TotalCost, List<IntegrationStep> DebugResults);