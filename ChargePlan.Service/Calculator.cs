using System.Diagnostics;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public class Calculator
{
    /// <summary>
    /// Calculate the end position in battery charge, and accumulated costs,
    /// for a test set of parameters.
    /// </summary>
    /// <param name="storageProfile">Capabilities of the system</param>
    /// <param name="demandProfiles">House demands. Typically the first one is the baseload, and additional ones may optionally be provided.</param>
    /// <param name="generationProfile">Generation profile based on global and celestial parameters</param>
    /// <param name="chargeProfile">Fixed charging from grid</param>
    /// <param name="pricingProfile">Unit price at each point over the period</param>
    /// <param name="currentState">Current battery energy level</param>
    /// <param name="chargePowerLimit">A hard set power limit for the grid charge period</param>
    /// <param name="shiftableDemands">Optional load demand which can be shifted to any point</param>
    /// <returns></returns>
    public Decision Calculate(
        StorageProfile storageProfile,
        DemandProfile mainDemandProfile,
        IEnumerable<DemandProfile> shiftableLoadDemandProfiles,
        GenerationProfile generationProfile,
        ChargeProfile chargeProfile,
        PricingProfile pricingProfile,
        CurrentState currentState,
        float? chargePowerLimit = null)
    {
        chargePowerLimit ??= storageProfile.MaxChargeKilowatts;

        TimeSpan step = TimeSpan.FromMinutes(60);
        DateTime startAt = mainDemandProfile.Values.Min(f => f.DateTime);
        DateTime endAt = mainDemandProfile.Values.Max(f => f.DateTime - step);

        var demandSplines = (new IInterpolation[] {
            mainDemandProfile.AsSpline(CubicSpline.InterpolateAkima) })
            .Concat(shiftableLoadDemandProfiles.Select(demandProfile => demandProfile.AsSpline(StepInterpolation.Interpolate)))
            .ToArray();

        var generationSpline = generationProfile.AsSpline(CubicSpline.InterpolateAkima);
        var chargeSpline = chargeProfile.AsSpline(StepInterpolation.Interpolate);
        var pricingSpline = pricingProfile.AsSpline(StepInterpolation.Interpolate);

        float overcharge = 0.0f;
        float undercharge = 0.0f;
        float cost = 0.0f;

        CurrentState integral = currentState with { DateTime = startAt };

        List<IntegrationStep> debugResults = new();

        while (integral.DateTime <= endAt)
        {
            double from = (integral.DateTime).AsTotalHours();
            double to = (integral.DateTime + step).AsTotalHours();

            float unitPrice = (float)pricingSpline.Interpolate(from);
            float demandEnergy = (float)demandSplines.Select(f => Math.Max(0.0f, f.Integrate(from, to))).Sum();
            float generationEnergy = (float)generationSpline.Integrate(from, to);
            float chargeEnergy = (float)
                Math.Max(
                    Math.Min(chargeSpline.Integrate(from, to),
                        Math.Min(storageProfile.MaxChargeKilowatts, chargePowerLimit ?? storageProfile.MaxChargeKilowatts)
                ), 0.0f);

            // House demand fulfilled from generation first.
            // If this results in negative demand, this is excess solar, so pass it back to the generationEnergy.
            float netDemandEnergy = demandEnergy - generationEnergy;
            float netGenerationEnergy = generationEnergy;
            if (netDemandEnergy < 0.0f)
            {
                netGenerationEnergy = -netDemandEnergy;
                netDemandEnergy = 0.0f;
            }

            // Process battery net energy in the order gen,charge,demand.
            // Net off any surplus before it goes in.
            var generation = integral.Add(netGenerationEnergy, storageProfile.CapacityKilowattHrs);
            integral = generation.NewState;
            var charge = integral.Add(chargeEnergy, storageProfile.CapacityKilowattHrs);
            integral = charge.NewState;

            // If this is a period of grid charging, then no drawdown from battery
            // can be used for the demand.
            var demand = integral.Pull(netDemandEnergy, isGridCharge: chargeEnergy > 0.0f);
            integral = demand.NewState;

            cost += (charge.Added + demand.Shortfall) * unitPrice;
            undercharge += demand.Shortfall;
            overcharge += generation.Unused;

            integral = integral with { DateTime = integral.DateTime + step };

            debugResults.Add(new(integral.DateTime, integral.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, cost, undercharge, overcharge));
        }

//        Debug.WriteLine($"Charge rate: {chargePowerLimit} Undercharge: {undercharge} Overcharge: {overcharge} Cost: £{cost.ToString("F2")}");

        return new Decision(chargePowerLimit, undercharge, overcharge, Math.Round((decimal)cost, 2), debugResults);
    }
}

public static class DateTimeExtensions
{
    public static double AsTotalHours(this DateTime dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;
}