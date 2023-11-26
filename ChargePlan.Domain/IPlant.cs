using ChargePlan.Domain.Exceptions;

namespace ChargePlan.Domain;

public abstract record IPlant(PlantIntegration LastIntegration, PlantState State)
{
    /// <summary>
    /// Perform an interation of the plant by processing energy and battery state.
    /// </summary>
    /// <param name="solarEnergy">Amount of solar energy available, before considering plant capacity</param>
    /// <param name="chargeEnergy">Amount of grid charge energy available, before considering plant capacity</param>
    /// <param name="demandEnergy">House demand</param>
    /// <param name="period">Period of the integration; used for conversions between energy and power</param>
    /// <param name="batteryDischargeOverrideKw">Artificially limit the battery discharge. Used when optimising I2R losses.</param>
    /// <returns></returns>
    public abstract IPlant IntegratedBy(float solarEnergy, float chargeEnergy, float demandEnergy, TimeSpan period, float? batteryDischargeOverrideKw = null);

    /// <summary>
    /// The charge rate for the battery in kW at a given scalar value (1.0 represents maximum)
    /// </summary>
    public abstract float ChargeRateAtScalar(float scalarValue);

    /// <summary>
    /// The discharge rate for the battery in kW at a given scalar value (1.0 represents maximum)
    /// </summary>
    public abstract float DischargeRateAtScalar(float scalarValue);

    /// <summary>
    /// Take a charge rate (Watts/Kw) and modify it by a scalar, but capping it at the maximum charge rate.
    /// </summary>
    public float? ChargeRateWithSafetyFactor(float? chargeRate, float safetyScalarValue)
        => chargeRate == null ? null : Math.Min(chargeRate.Value * safetyScalarValue, ChargeRateAtScalar(1.0f));

    public void ThrowIfInvalid()
    {
        if (float.IsNaN(LastIntegration.GridCharged) || float.IsFinite(LastIntegration.GridCharged) == false) throw new InvalidStateException($"Invalid LastIntegration.GridCharged: {LastIntegration}");
        if (float.IsNaN(LastIntegration.GridExport) || float.IsFinite(LastIntegration.GridExport) == false) throw new InvalidStateException($"Invalid LastIntegration.GridExport: {LastIntegration}");
        if (float.IsNaN(LastIntegration.Shortfall) || float.IsFinite(LastIntegration.Shortfall) == false) throw new InvalidStateException($"Invalid LastIntegration.Shortfall: {LastIntegration}");
        if (float.IsNaN(LastIntegration.Wasted) || float.IsFinite(LastIntegration.Wasted) == false) throw new InvalidStateException($"Invalid LastIntegration.Wasted: {LastIntegration}");
    }
}

public record PlantIntegration(float GridCharged, float GridExport, float Shortfall, float Wasted);
public record PlantState(float BatteryEnergy);