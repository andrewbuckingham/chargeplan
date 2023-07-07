using System.Diagnostics;
using ChargePlan.Domain.Exceptions;

namespace ChargePlan.Domain;

public record Calculator(IPlant PlantTemplate)
{
    public static readonly TimeSpan TimeStep = TimeSpan.FromMinutes(5);

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
        IInterpolationFactory interpolationFactory,
        PlantState initialState,
        float? chargePowerLimit = null,
        DateTimeOffset? explicitStartDate = null)
    {
        IPlant plant = PlantTemplate with { State = initialState };

        TimeSpan step = TimeStep;

        DateTimeOffset startAt = (explicitStartDate ?? new DateTimeOffset(baseloadDemandProfile.Starting).OrAtEarliest(DateTimeOffset.Now)).ToClosestHour();
        DateTimeOffset endAt = baseloadDemandProfile.Until - step;

        if (startAt < baseloadDemandProfile.Starting) throw new InvalidStateException("Cannot start before baseload demand timescale");

        var demandSplines = Enumerable.Empty<(IInterpolation Interpolation, IDemandProfile Profile)>()
            .Append((Interpolation: baseloadDemandProfile.AsSpline(interpolationFactory.InterpolateBaseload), Profile: baseloadDemandProfile))
            .Concat(specificDemandProfiles.Select(demandProfile => (Interpolation: demandProfile.AsSpline(interpolationFactory.InterpolateShiftableDemand), Profile: demandProfile)))
            .ToArray();

        var generationSpline = generationProfile.AsSplineOrZero(interpolationFactory.InterpolateGeneration);
        var chargeSpline = chargeProfile.AsSplineOrZero(interpolationFactory.InterpolateCharging);
        var pricingSpline = pricingProfile.AsSpline(interpolationFactory.InterpolatePricing);
        var exportSpline = exportProfile.AsSplineOrZero(interpolationFactory.InterpolateExport);

        float overcharge = 0.0f;
        float undercharge = 0.0f;
        float cost = 0.0f;

        DateTimeOffset now = startAt.ToClosestHour();

        List<IntegrationStep> debugResults = new();

        while (now <= endAt)
        {
            double from = (now).AsTotalHours();
            double to = (now + step).AsTotalHours();

            var demandEnergies = demandSplines.Select(f => (Energy: Math.Max(0.0f, f.Interpolation.Integrate(from, to)), Profile: f.Item2)).ToArray();

            float unitPrice = Math.Max(0.0f, (float)pricingSpline.Interpolate(from));
            float exportPrice = Math.Max(0.0f, (float)exportSpline.Interpolate(from));
            float demandEnergy = (float)demandEnergies.Select(f => f.Energy).Sum();
            float generationEnergy = Math.Max(0.0f, (float)generationSpline.Integrate(from, to));
            float chargeEnergy = (float)Math.Max(0.0f, Math.Min(chargeSpline.Integrate(from, to), step.Energy(chargePowerLimit ?? float.MaxValue)));

            plant = plant.IntegratedBy(generationEnergy, chargeEnergy, demandEnergy, step);

            cost += (plant.LastIntegration.GridCharged + plant.LastIntegration.Shortfall) * unitPrice;
            cost -= (plant.LastIntegration.GridExport) * exportPrice;
            undercharge += plant.LastIntegration.Shortfall;
            overcharge += plant.LastIntegration.Wasted + plant.LastIntegration.GridExport;

            now += step;

            debugResults.Add(new(
                now,
                plant.State.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, plant.LastIntegration.GridExport, cost, undercharge, overcharge,
                new(step.Power(generationEnergy)),
                demandEnergies.Select(f => new IntegrationStepDemandEnergy(f.Profile.Name, f.Profile.Type, (float)f.Energy)).ToArray()
            ));
        }

        var overchargeAndUnderchargePeriods = CalculateOverchargePeriods(debugResults);

        return new Evaluation(
            chargePowerLimit,
            Math.Round((decimal)cost, 2),
            debugResults,
            overchargeAndUnderchargePeriods.Item1,
            overchargeAndUnderchargePeriods.Item2);
    }

    private enum Direction { Indeterminate = 0, Undercharge, Overcharge };
    private record Accumulator(float Amount, Direction Direction, DateTimeOffset Since);

    private (List<OverchargePeriod> Overcharge, List<UnderchargePeriod> Undercharge) CalculateOverchargePeriods(IEnumerable<IntegrationStep> integrationSteps)
    {
        List<OverchargePeriod> overchargePeriods = new();
        List<UnderchargePeriod> underchargePeriods = new();
        Accumulator accumulator = new(0.0f, Direction.Indeterminate, integrationSteps.First().DateTime - TimeStep);

        var sourceData = integrationSteps
            .Zip(integrationSteps.Skip(1).Append(null))
            .Select(pair => (
                HasUnderchargeOccurred: pair.Second?.CumulativeUndercharge > pair.First.CumulativeUndercharge,
                HasOverchargeOccurred: pair.Second?.CumulativeOvercharge > pair.First.CumulativeOvercharge,
                First: pair.First,
                Second: pair.Second
            ));

        foreach (var pair in sourceData)
        {            
            if (pair.HasOverchargeOccurred) accumulator = accumulator with
            {
                Direction = Direction.Overcharge,
                Amount = accumulator.Amount + ((pair.Second?.CumulativeOvercharge ?? pair.First.CumulativeOvercharge) - pair.First.CumulativeOvercharge)
            };
            else if (pair.HasUnderchargeOccurred) accumulator = accumulator with
            {
                Direction = Direction.Undercharge,
                Amount = accumulator.Amount + ((pair.Second?.CumulativeUndercharge ?? pair.First.CumulativeUndercharge) - pair.First.CumulativeUndercharge)
            };
            else if (accumulator.Direction == Direction.Overcharge)
            {
                // No longer overcharge, but we were previously in a period of such. Add and clear down.
                overchargePeriods.Add(new OverchargePeriod(accumulator.Since, pair.First.DateTime, accumulator.Amount));
                accumulator = new(0.0f, Direction.Indeterminate, pair.First.DateTime);
            }
            else if (accumulator.Direction == Direction.Undercharge)
            {
                // No longer overcharge, but we were previously in a period of such. Add and clear down.
                underchargePeriods.Add(new UnderchargePeriod(accumulator.Since, pair.First.DateTime, accumulator.Amount));
                accumulator = new(0.0f, Direction.Indeterminate, pair.First.DateTime);
            }
            else
            {
                accumulator = accumulator with { Direction = Direction.Indeterminate };
            }
        }

        return (overchargePeriods, underchargePeriods);
    }
}