public record PlantIntegration(float GridCharged, float Shortfall, float Wasted);

public record PlantState(float BatteryEnergy)
{
    /// <summary>
    /// Add some energy into the battery observing capacity limits.
    /// </summary>
    public (PlantState NewState, float Added, float Unused) Add(float energy, float energyLimit, float energyDeltaLimit)
    {
        float unusedDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit); // More power than system can cope with
        float unusedDueToEnergyLimit = Math.Max(0, (this.BatteryEnergy + energy) - energyLimit); // More energy than the battery can hold

        float deltaForBattery = Math.Min(energy, energyDeltaLimit);

        float newState = Math.Min(energyLimit, BatteryEnergy + deltaForBattery);

        return (
            this with { BatteryEnergy = newState },
            energy - (unusedDueToEnergyLimit + unusedDueToEnergyDeltaLimit),
            unusedDueToEnergyLimit + unusedDueToEnergyDeltaLimit
            );
    }

    /// <summary>
    /// Pull some energy from the battery observing where it is empty.
    /// </summary>
    /// <param name="isGridCharge">If true, this will not take from battery and return as shortfall.</param>
    public (PlantState NewState, float Pulled, float Shortfall) Pull(float energy, bool isGridCharge, float energyDeltaLimit)
    {
        if (isGridCharge) return (this, 0.0f, energy);

        float shortfallDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit);
        float shortfallDueToEmpty = -Math.Min(0, this.BatteryEnergy - energy);

        float deltaforBattery = Math.Min(energy, energyDeltaLimit);

        float newState = Math.Max(0.0f, this.BatteryEnergy - deltaforBattery);

        return (
            this with { BatteryEnergy = newState },
            energy - (shortfallDueToEmpty + shortfallDueToEnergyDeltaLimit),
            shortfallDueToEmpty + shortfallDueToEnergyDeltaLimit
            );
    }
}

public abstract record IPlant(PlantIntegration LastIntegration, PlantState State)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="solarEnergy">Amount of solar energy available, before considering plant capacity</param>
    /// <param name="chargeEnergy">Amount of grid charge energy available, before considering plant capacity</param>
    /// <param name="demandEnergy">House demand</param>
    /// <param name="period">Period of the integration; used for conversions between energy and power</param>
    /// <returns></returns>
    public abstract IPlant IntegratedBy(float solarEnergy, float chargeEnergy, float demandEnergy, TimeSpan period);

    /// <summary>
    /// The charge rate for the battery in kW at a given scalar value (1.0 represents maximum)
    /// </summary>
    public abstract float ChargeRateAtScalar(float scalarValue);
}