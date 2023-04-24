using System.Diagnostics;
using MathNet.Numerics.Interpolation;

public record Calculator(IPlant PlantTemplate)
{
    /// <summary>
    /// Calculate the end position in battery charge, and accumulated costs,
    /// for a test set of parameters.
    /// The starting point will be the Starting point in the baseload demand profile, or DateTime.Now, whichever is latest.
    /// </summary>
    /// <param name="baseloadDemandProfile">House demand baseload. Also defines the time bounds of the calculation. For specific loads (e.g. washing machines) use the shiftableLoadDemandProfiles instead.</param>
    /// <param name="generationProfile">Generation profile based on global and celestial parameters</param>
    /// <param name="chargeProfile">Fixed charging from grid</param>
    /// <param name="pricingProfile">Unit price at each point over the period</param>
    /// <param name="exportProfile">Unit price for export at each point over the period</param>
    /// <param name="initialState">Current battery energy level</param>
    /// <param name="chargePowerLimit">A hard set power limit for the grid charge period</param>
    /// <param name="specificDemandProfiles">Specific demands e.g. individual high-loads that are transient</param>
    /// <returns></returns>
    public Evaluation Calculate(
        IDemandProfile baseloadDemandProfile,
        IEnumerable<IDemandProfile> specificDemandProfiles,
        IGenerationProfile generationProfile,
        IChargeProfile chargeProfile,
        IPricingProfile pricingProfile,
        IExportProfile exportProfile,
        PlantState initialState,
        float? chargePowerLimit = null)
    {
        IPlant plant = PlantTemplate with { State = initialState };

        TimeSpan step = TimeSpan.FromMinutes(60);
        DateTime startAt = baseloadDemandProfile.Starting;
        DateTime endAt = baseloadDemandProfile.Until - step;

        var demandSplines = (new IInterpolation[] {
            baseloadDemandProfile.AsSpline(CubicSpline.InterpolateAkima) })
            .Concat(specificDemandProfiles.Select(demandProfile => demandProfile.AsSpline(StepInterpolation.Interpolate)))
            .ToArray();

        var generationSpline = generationProfile.AsSpline(CubicSpline.InterpolateAkima);
        var chargeSpline = chargeProfile.AsSpline(StepInterpolation.Interpolate);
        var pricingSpline = pricingProfile.AsSpline(StepInterpolation.Interpolate);
        var exportSpline = exportProfile.AsSpline(StepInterpolation.Interpolate);

        float overcharge = 0.0f;
        float undercharge = 0.0f;
        float cost = 0.0f;

        if (startAt < DateTime.Now) startAt = DateTime.Now;
        DateTime now = startAt.ToClosestHour();

        List<IntegrationStep> debugResults = new();

        while (now <= endAt)
        {
            double from = (now).AsTotalHours();
            double to = (now + step).AsTotalHours();

            float unitPrice = Math.Max(0.0f, (float)pricingSpline.Interpolate(from));
            float exportPrice = Math.Max(0.0f, (float)exportSpline.Interpolate(from));
            float demandEnergy = (float)demandSplines.Select(f => Math.Max(0.0f, f.Integrate(from, to))).Sum();
            float generationEnergy = Math.Max(0.0f, (float)generationSpline.Integrate(from, to));
            float chargeEnergy = (float)Math.Max(0.0f, Math.Min(chargeSpline.Integrate(from, to), step.Energy(chargePowerLimit ?? float.MaxValue)));

            plant = plant.IntegratedBy(generationEnergy, chargeEnergy, demandEnergy, step);

            cost += (plant.LastIntegration.GridCharged + plant.LastIntegration.Shortfall) * unitPrice;
            cost -= (plant.LastIntegration.GridExport) * exportPrice;
            undercharge += plant.LastIntegration.Shortfall;
            overcharge += plant.LastIntegration.Wasted;

            now += step;

            debugResults.Add(new(now, plant.State.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, plant.LastIntegration.GridExport, cost, undercharge, overcharge));
        }

        return new Evaluation(chargePowerLimit, undercharge, overcharge, Math.Round((decimal)cost, 2), debugResults);
    }
}
