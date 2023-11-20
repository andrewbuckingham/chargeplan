namespace ChargePlan.Domain.Plant;

/// <summary>
/// A representation of a Gen2 GivEnergy hybrid inverter
/// </summary>
/// <param name="CapacityKilowattHrs"></param>
/// <param name="MaxChargeKilowatts"></param>
/// <param name="MaxDischargeKilowatts"></param>
/// <param name="MaxThroughputKilowatts"></param>
/// <param name="BatteryRoundRoundTripEfficiencyScalar"></param>
/// <param name="BatteryI2RLossesScalar">Scalar amount of the peak throughput that's lost as I2R. E.g. 1.0 would be 100% loss (i.e. impossible) at MaxDischargeKilowatts</param>
/// <param name="DepthOfDischargePercent"></param>
/// <param name="ReservePercent"></param>
public record Hy36(
    float CapacityKilowattHrs,
    float MaxChargeKilowatts,
    float MaxDischargeKilowatts,
    float MaxThroughputKilowatts,
    float BatteryRoundRoundTripEfficiencyScalar,
    float BatteryI2RLossesScalar,
    int DepthOfDischargePercent,
    int ReservePercent) : IPlant(new(0.0f, 0.0f, 0.0f, 0.0f), new(0.0f))
{
    private float UpperBoundsKilowattHrs => (float)DepthOfDischargePercent * CapacityKilowattHrs / 100.0f;
    private float LowerBoundsKilowattHrs => (float)ReservePercent * CapacityKilowattHrs / 100.0f;

    private float EnergyAdjustedByChargeEfficiency(float currentPowerKilowatts, float energy) => Math.Max(0.0f,
        energy
        - energy * (1 - BatteryRoundRoundTripEfficiencyScalar) / 2
        - energy * BatteryI2RLossesScalar * (currentPowerKilowatts / MaxChargeKilowatts) * (currentPowerKilowatts / MaxChargeKilowatts)
        );
    private float EnergyAdjustedByDischargeEfficiency(float currentPowerKilowatts, float energy) => Math.Max(0.0f,
        energy
        + energy * (1 - BatteryRoundRoundTripEfficiencyScalar) / 2
        + energy * BatteryI2RLossesScalar * (currentPowerKilowatts / MaxChargeKilowatts) * (currentPowerKilowatts / MaxChargeKilowatts)
        );

    public override float ChargeRateAtScalar(float atScalarValue) => MaxChargeKilowatts * Math.Max(0.0f, Math.Min(1.0f, atScalarValue));
    public override float DischargeRateAtScalar(float atScalarValue) => MaxDischargeKilowatts * Math.Max(0.0f, Math.Min(1.0f, atScalarValue));

    public override IPlant IntegratedBy(float solarEnergy, float chargeEnergy, float demandEnergy, TimeSpan period, float? batteryDischargeOverrideKw)
    {
        float wasted = 0.0f;

        if (float.IsFinite(solarEnergy) == false) throw new ArgumentOutOfRangeException(nameof(solarEnergy), solarEnergy, "Must be finite");
        if (float.IsFinite(chargeEnergy) == false) throw new ArgumentOutOfRangeException(nameof(chargeEnergy), chargeEnergy, "Must be finite");
        if (float.IsFinite(demandEnergy) == false) throw new ArgumentOutOfRangeException(nameof(demandEnergy), demandEnergy, "Must be finite");
        if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period), period, "Must be positive");

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
        var afterSolar = AddTo(newState, solarEnergy, remainingBatteryChargeThroughput, period);
        newState = afterSolar.NewState;
        remainingBatteryChargeThroughput -= afterSolar.Added;

        // Grid charge second.
        var afterGrid = AddTo(newState, chargeEnergy, remainingBatteryChargeThroughput, period);
        newState = afterGrid.NewState;
        remainingBatteryChargeThroughput -= afterGrid.Added;

        // Finally, pull energy out of battery for demand.
        // NB if this is a period of grid charging, then no drawdown from battery can be used for the demand.
        var afterDemand = PullFrom(newState, demandEnergy, afterGrid.Added > 0.0f, period.Energy(Math.Min(MaxDischargeKilowatts, batteryDischargeOverrideKw ?? MaxDischargeKilowatts)), period);
        newState = afterDemand.NewState;

        return this with
        {
            State = newState,
            LastIntegration = new(
                afterGrid.Added,
                afterSolar.Unused, // Any leftover solar, which is still within the Plant throughput capacity, is Export.
                afterDemand.Shortfall,
                wasted)
        };
    }

    /// <summary>
    /// Add some energy into the battery observing capacity limits.
    /// </summary>
    private (PlantState NewState, float Added, float Unused) AddTo(PlantState state, float energy, float energyDeltaLimit, TimeSpan period)
    {
        var minusEfficiency = (float f) => EnergyAdjustedByChargeEfficiency(period.Power(energy), f);

        float unusedDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit); // More power than system can cope with
        float unusedDueToEnergyLimit = Math.Max(0, (state.BatteryEnergy + energy) - UpperBoundsKilowattHrs); // More energy than the battery can hold

        float deltaForBattery = energy - unusedDueToEnergyDeltaLimit - unusedDueToEnergyLimit;

        float newState = Math.Min(UpperBoundsKilowattHrs, state.BatteryEnergy + minusEfficiency(deltaForBattery));

        return (
            state with { BatteryEnergy = newState },
            deltaForBattery,
            unusedDueToEnergyLimit + unusedDueToEnergyDeltaLimit
            );
    }

    /// <summary>
    /// Pull some energy from the battery observing where it is empty.
    /// </summary>
    /// <param name="isGridCharge">If true, this will not take from battery and return as shortfall.</param>
    private (PlantState NewState, float Pulled, float Shortfall) PullFrom(PlantState state, float energy, bool isGridCharge, float energyDeltaLimit, TimeSpan period)
    {
        if (isGridCharge) return (state, 0.0f, energy);

        var minusEfficiency = (float f) => EnergyAdjustedByDischargeEfficiency(period.Power(energy), f);

        float shortfallDueToEnergyDeltaLimit = Math.Max(0, energy - energyDeltaLimit);
        float shortfallDueToEmpty = -Math.Min(0, (state.BatteryEnergy - LowerBoundsKilowattHrs) - minusEfficiency(energy));

        float deltaForBattery = energy - shortfallDueToEnergyDeltaLimit - shortfallDueToEmpty;

        float newState = Math.Max(LowerBoundsKilowattHrs, state.BatteryEnergy - minusEfficiency(deltaForBattery));

        return (
            state with { BatteryEnergy = newState },
            deltaForBattery,
            shortfallDueToEmpty + shortfallDueToEnergyDeltaLimit
            );
    }
}