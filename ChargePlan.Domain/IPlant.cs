public abstract record IPlant(PlantIntegration LastIntegration, PlantState State)
{
    /// <summary>
    /// Perform an interation of the plant by processing energy and battery state.
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

public record PlantIntegration(float GridCharged, float GridExport, float Shortfall, float Wasted);
public record PlantState(float BatteryEnergy);