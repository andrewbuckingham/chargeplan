using System.Diagnostics;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public class Calculator
{
    public Decision Calculate(
        StorageProfile storageProfile,
        DemandProfile demandProfile,
        GenerationProfile generationProfile,
        ChargeProfile chargeProfile,
        PricingProfile pricingProfile,
        CurrentState currentState,
        float? chargePowerLimit)
    {
        var demandSpline = CubicSpline.InterpolateAkima(demandProfile.Values.Select(f => (double)f.DateTime.AsTotalHours()), demandProfile.Values.Select(f => (double)f.Power));
        var generationSpline = CubicSpline.InterpolateAkima(generationProfile.Values.Select(f => (double)f.DateTime.AsTotalHours()), generationProfile.Values.Select(f => (double)f.Power));
        var chargeSpline = StepInterpolation.Interpolate(chargeProfile.Values.Select(f => (double)f.DateTime.AsTotalHours()), chargeProfile.Values.Select(f => (double)f.Power));
        var pricingSpline = StepInterpolation.Interpolate(pricingProfile.Values.Select(f => (double)f.DateTime.AsTotalHours()), pricingProfile.Values.Select(f => (double)f.PricePerUnit));
        
        TimeSpan step = TimeSpan.FromMinutes(60);

        DateTime startAt = demandProfile.Values.Min(f => f.DateTime);
        DateTime endAt = demandProfile.Values.Max(f => f.DateTime - step);

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
            float demandEnergy = (float)demandSpline.Integrate(from, to);
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
            var demand = integral.Pull(netDemandEnergy);
            integral = demand.NewState;

            cost += (charge.Added + demand.Shortfall) * unitPrice;
            undercharge += demand.Shortfall;
            overcharge += generation.Unused;

            integral = integral with { DateTime = integral.DateTime + step };

            debugResults.Add(new(integral.DateTime, integral.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, cost, undercharge, overcharge));
        }

        Debug.WriteLine($"Charge rate: {chargePowerLimit ?? storageProfile.MaxChargeKilowatts} Undercharge: {undercharge} Overcharge: {overcharge} Cost: £{cost.ToString("F2")}");

        return new Decision(chargePowerLimit, undercharge, overcharge, Math.Round((decimal)cost, 2), debugResults);
    }
}

public static class DateTimeExtensions
{
    public static double AsTotalHours(this DateTime dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;
}