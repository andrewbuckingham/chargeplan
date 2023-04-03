namespace ChargePlan.Service;

public record CurrentState(DateTime DateTime, float BatteryEnergy)
{
    /// <summary>
    /// Add some energy into the battery observing capacity limits.
    /// </summary>
    public (CurrentState NewState, float Added, float Unused) Add(float energy, float limit)
    {
        float newState = BatteryEnergy + energy;
        if (newState > limit)
        {
            return (this with { BatteryEnergy = limit }, energy - (newState - limit), newState - limit);
        }
        else
        {
            return (this with { BatteryEnergy = newState }, energy, 0.0f);
        }
    }

    /// <summary>
    /// Pull some neergy from the battery observing where it is empty.
    /// </summary>
    /// <param name="isGridCharge">If true, this will not take from battery and return as shortfall.</param>
    public (CurrentState NewState, float Pulled, float Shortfall) Pull(float energy, bool isGridCharge)
    {
        if (isGridCharge) return (this, 0.0f, energy);

        float newState = BatteryEnergy - energy;
        if (newState < 0)
        {
            return (this with { BatteryEnergy = 0.0f }, this.BatteryEnergy, energy - this.BatteryEnergy);
        }
        else
        {
            return (this with { BatteryEnergy = newState }, energy, 0.0f);
        }
    }
}