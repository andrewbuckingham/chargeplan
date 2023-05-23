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
    IntegrationStepDemandEnergy[] DemandEnergies
);

public record IntegrationStepDemandEnergy(string Name, string Type, float Energy);