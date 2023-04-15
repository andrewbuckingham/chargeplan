public record Hy36(float CapacityKilowattHrs,
        float MaxChargeKilowatts,
        float MaxDischargeKilowatts,
        float MaxThroughputKilowatts) : IPlant(new(0.0f, 0.0f, 0.0f), new(0.0f))
{
    public override float ChargeRateAtScalar(float atScalarValue) => MaxChargeKilowatts * atScalarValue;

    public override IPlant IntegratedBy(float solarEnergy, float chargeEnergy, float demandEnergy, TimeSpan period)
    {
        float wasted = 0.0f;

        // ----
        // Set some limits for energy being processed.

        // Cap input from solar & grid to the throughput.
        float remainingSystemThroughput = period.Energy(MaxThroughputKilowatts);

        // Cap solar at throughput.
        remainingSystemThroughput -= solarEnergy;
        if (remainingSystemThroughput < 0.0f)
        {
            solarEnergy += remainingSystemThroughput; // Reduce usable solar to effectively be the MaxThroughput value
            wasted += -remainingSystemThroughput;
            remainingSystemThroughput = 0.0f;
        }

        // Cap charge at throughput.
        remainingSystemThroughput -= chargeEnergy;
        if (remainingSystemThroughput < 0.0f)
        {
            chargeEnergy += remainingSystemThroughput; // Unused grid charge doesn't matter, other than calc'ing costs.
            remainingSystemThroughput = 0.0f;
        }

        // House demand fulfilled from solar first.
        if (solarEnergy > demandEnergy)
        {
            solarEnergy -= demandEnergy;
            demandEnergy = 0.0f;
        }
        else
        {
            demandEnergy -= solarEnergy;
            solarEnergy = 0;
        }

        // ----
        // Now the energy is limited, process it into and out of the battery.

        float remainingBatteryChargeThroughput = period.Energy(MaxChargeKilowatts);

        PlantState newState = State;

        // Solar first.
        var afterSolar = AddTo(newState, solarEnergy, CapacityKilowattHrs, remainingBatteryChargeThroughput);
        newState = afterSolar.NewState;
        remainingBatteryChargeThroughput -= afterSolar.Added;

        // Grid charge second.
        var afterGrid = AddTo(newState, chargeEnergy, CapacityKilowattHrs, remainingBatteryChargeThroughput);
        newState = afterGrid.NewState;
        remainingBatteryChargeThroughput -= afterGrid.Added;

        // Finally, pull energy out of battery for demand.
        // NB if this is a period of grid charging, then no drawdown from battery can be used for the demand.
        var afterDemand = PullFrom(newState, demandEnergy, afterGrid.Added > 0.0f, period.Energy(MaxDischargeKilowatts));
        newState = afterDemand.NewState;

        return this with
        {
            State = newState,
            LastIntegration = new(afterGrid.Added, afterDemand.Shortfall, afterSolar.Unused + wasted)
        };
    }

    /// <summary>
    /// Add some energy into the battery observing capacity limits.
    /// </summary>
    private static (PlantState NewState, float Added, float Unused) AddTo(PlantState state, float energy, float energyLimit, float energyDeltaLimit)
    {
        float unusedDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit); // More power than system can cope with
        float unusedDueToEnergyLimit = Math.Max(0, (state.BatteryEnergy + energy) - energyLimit); // More energy than the battery can hold

        float deltaForBattery = Math.Min(energy, energyDeltaLimit);

        float newState = Math.Min(energyLimit, state.BatteryEnergy + deltaForBattery);

        return (
            state with { BatteryEnergy = newState },
            energy - (unusedDueToEnergyLimit + unusedDueToEnergyDeltaLimit),
            unusedDueToEnergyLimit + unusedDueToEnergyDeltaLimit
            );
    }

    /// <summary>
    /// Pull some energy from the battery observing where it is empty.
    /// </summary>
    /// <param name="isGridCharge">If true, this will not take from battery and return as shortfall.</param>
    private static (PlantState NewState, float Pulled, float Shortfall) PullFrom(PlantState state, float energy, bool isGridCharge, float energyDeltaLimit)
    {
        if (isGridCharge) return (state, 0.0f, energy);

        float shortfallDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit);
        float shortfallDueToEmpty = -Math.Min(0, state.BatteryEnergy - energy);

        float deltaforBattery = Math.Min(energy, energyDeltaLimit);

        float newState = Math.Max(0.0f, state.BatteryEnergy - deltaforBattery);

        return (
            state with { BatteryEnergy = newState },
            energy - (shortfallDueToEmpty + shortfallDueToEnergyDeltaLimit),
            shortfallDueToEmpty + shortfallDueToEnergyDeltaLimit
            );
    }
}