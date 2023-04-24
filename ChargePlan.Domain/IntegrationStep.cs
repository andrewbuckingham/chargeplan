public record IntegrationStep(
    DateTime DateTime,
    float BatteryEnergy,
    float DemandEnergy,
    float GenerationEnergy,
    float ChargeEnergy,
    float ExportEnergy,
    float CumulativeCost,
    float CumulativeUndercharge,
    float CumulativeOvercharge
);