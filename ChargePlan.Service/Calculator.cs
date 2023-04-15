using System.Diagnostics;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public record Calculator(IPlant PlantTemplate)
{
    /// <summary>
    /// Calculate the end position in battery charge, and accumulated costs,
    /// for a test set of parameters.
    /// </summary>
    /// <param name="mainDemandProfile">House demand baseload. Also defines the time bounds of the calculation. For specific loads (e.g. washing machines) use the shiftableLoadDemandProfiles instead.</param>
    /// <param name="generationProfile">Generation profile based on global and celestial parameters</param>
    /// <param name="chargeProfile">Fixed charging from grid</param>
    /// <param name="pricingProfile">Unit price at each point over the period</param>
    /// <param name="initialState">Current battery energy level</param>
    /// <param name="chargePowerLimit">A hard set power limit for the grid charge period</param>
    /// <param name="shiftableLoadDemandProfiles">Optional load demand which can be shifted to any point</param>
    /// <returns></returns>
    public Decision Calculate(
        DemandProfile mainDemandProfile,
        IEnumerable<DemandProfile> shiftableLoadDemandProfiles,
        GenerationProfile generationProfile,
        ChargeProfile chargeProfile,
        PricingProfile pricingProfile,
        PlantState initialState,
        float? chargePowerLimit = null)
    {
        IPlant plant = PlantTemplate with { State = initialState };

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

        DateTime now = startAt;

        List<IntegrationStep> debugResults = new();

        while (now <= endAt)
        {
            double from = (now).AsTotalHours();
            double to = (now + step).AsTotalHours();

            float unitPrice = Math.Max(0.0f, (float)pricingSpline.Interpolate(from));
            float demandEnergy = (float)demandSplines.Select(f => Math.Max(0.0f, f.Integrate(from, to))).Sum();
            float generationEnergy = Math.Max(0.0f, (float)generationSpline.Integrate(from, to));
            float chargeEnergy = (float)Math.Max(0.0f, Math.Min(chargeSpline.Integrate(from, to), step.Energy(chargePowerLimit ?? float.MaxValue)));

            plant = plant.IntegratedBy(generationEnergy, chargeEnergy, demandEnergy, step);

            cost += (plant.LastIntegration.GridCharged + plant.LastIntegration.Shortfall) * unitPrice;
            undercharge += plant.LastIntegration.Shortfall;
            overcharge += plant.LastIntegration.Wasted;

            now += step;

            debugResults.Add(new(now, plant.State.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, cost, undercharge, overcharge));
        }

        return new Decision(chargePowerLimit, undercharge, overcharge, Math.Round((decimal)cost, 2), debugResults);
    }
}

public static class DateTimeExtensions
{
    public static double AsTotalHours(this DateTime dateTime) => (double)dateTime.Ticks / (double)TimeSpan.FromHours(1.0).Ticks;
}