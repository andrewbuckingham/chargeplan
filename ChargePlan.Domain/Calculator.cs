using System.Diagnostics;
using ChargePlan.Domain.Exceptions;

namespace ChargePlan.Domain;

/// <summary>
/// 
/// </summary>
/// <param name="PlantTemplate"></param>
/// <param name="BaseloadDemandProfile">House demand baseload. Also defines the time bounds of the calculation. For specific loads (e.g. washing machines) use the shiftableLoadDemandProfiles instead.</param>
/// <param name="GenerationProfile">Generation profile based on global and celestial parameters</param>
/// <param name="ChargeProfile">Fixed charging from grid</param>
/// <param name="PricingProfile">Unit price at each point over the period</param>
/// <param name="ExportProfile">Unit price for export at each point over the period</param>
/// <param name="SpecificDemandProfiles">Specific demands e.g. individual high-loads that are transient</param>
public record Calculator
{
    public IPlant PlantTemplate { get; init; }
    public IDemandProfile BaseloadDemandProfile { get; init; }
    public IEnumerable<IDemandProfile> SpecificDemandProfiles { get; init; }
    public IGenerationProfile GenerationProfile { get; init; }
    public IChargeProfile ChargeProfile { get; init; }
    public IPricingProfile PricingProfile { get; init; }
    public IExportProfile ExportProfile { get; init; }
    public IInterpolationFactory InterpolationFactory { get; init; }

    /// <summary>
    /// All the demand profiles (baseload and specific) as demand splines, for convenience of calculating energies.
    /// </summary>
    private Lazy<(IInterpolation Interpolation, IDemandProfile Profile)[]> DemandSplines { get; init; }

    private Lazy<IInterpolation> GenerationSpline { get; init; }
    private Lazy<IInterpolation> ChargeSpline { get; init; }
    private Lazy<IInterpolation> PricingSpline { get; init; }
    private Lazy<IInterpolation> ExportSpline { get; init; }
    
    public Calculator(
        IPlant PlantTemplate,
        IDemandProfile BaseloadDemandProfile,
        IEnumerable<IDemandProfile> SpecificDemandProfiles,
        IGenerationProfile GenerationProfile,
        IChargeProfile ChargeProfile,
        IPricingProfile PricingProfile,
        IExportProfile ExportProfile,
        IInterpolationFactory InterpolationFactory
    )
    {
        this.PlantTemplate = PlantTemplate;
        this.BaseloadDemandProfile = BaseloadDemandProfile;
        this.SpecificDemandProfiles = SpecificDemandProfiles;
        this.GenerationProfile = GenerationProfile;
        this.ChargeProfile = ChargeProfile;
        this.PricingProfile = PricingProfile;
        this.ExportProfile = ExportProfile;
        this.InterpolationFactory = InterpolationFactory;

        DemandSplines = new(() => Enumerable.Empty<(IInterpolation Interpolation, IDemandProfile Profile)>()
            .Append((Interpolation: BaseloadDemandProfile.AsSpline(InterpolationFactory.InterpolateBaseload), Profile: BaseloadDemandProfile))
            .Concat(SpecificDemandProfiles.Select(demandProfile => (Interpolation: demandProfile.AsSpline(InterpolationFactory.InterpolateShiftableDemand), Profile: demandProfile)))
            .ToArray());

        GenerationSpline = new(() => GenerationProfile.AsSplineOrZero(InterpolationFactory.InterpolateGeneration));
        ChargeSpline = new(() => ChargeProfile.AsSplineOrZero(InterpolationFactory.InterpolateCharging));
        PricingSpline = new(() => PricingProfile.AsSpline(InterpolationFactory.InterpolatePricing));
        ExportSpline = new(() => ExportProfile.AsSplineOrZero(InterpolationFactory.InterpolateExport));
    }

    /// <summary>
    /// Calculate the end position in battery charge, and accumulated costs,
    /// for a test set of parameters.
    /// The starting point will be the Starting point in the baseload demand profile, or DateTime.Now, whichever is latest.
    /// </summary>
    /// <param name="initialState">Current battery energy level</param>
    /// <param name="chargePowerLimit">A hard set power limit for the grid charge period</param>
    /// <param name="dishargePowerLimit">A hard set power limit for when discharging from the battery</param>
    /// <returns></returns>
    public Evaluation Calculate(
        PlantState initialState,
        TimeSpan timeStep,
        float? chargePowerLimit = null,
        float? dischargePowerLimit = null,
        DateTimeOffset? explicitStartDate = null)
    {
        IPlant plant = PlantTemplate with { State = initialState };

        TimeSpan step = timeStep;

        DateTimeOffset startAt = (explicitStartDate ?? new DateTimeOffset(BaseloadDemandProfile.Starting).OrAtEarliest(DateTimeOffset.Now)).ToClosestHour();
        DateTimeOffset endAt = BaseloadDemandProfile.Until - step;

        if (startAt < BaseloadDemandProfile.Starting) throw new InvalidStateException("Cannot start before baseload demand timescale");
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Timestep must be positive");

        float overcharge = 0.0f;
        float undercharge = 0.0f;
        float cost = 0.0f;

        DateTimeOffset now = startAt.ToClosestHour();

        List<IntegrationStep> debugResults = new();

        while (now <= endAt)
        {
            double from = (now).AsTotalHours();
            double to = (now + step).AsTotalHours();

            var demandEnergies = DemandSplines.Value.Select(f => (Energy: Math.Max(0.0f, f.Interpolation.Integrate(from, to)), Profile: f.Item2)).ToArray();

            float unitPrice = Math.Max(0.0f, (float)PricingSpline.Value.Interpolate(from));
            float exportPrice = Math.Max(0.0f, (float)ExportSpline.Value.Interpolate(from));
            float demandEnergy = (float)demandEnergies.Select(f => f.Energy).Sum();
            float generationEnergy = Math.Max(0.0f, (float)GenerationSpline.Value.Integrate(from, to));
            float chargeEnergy = (float)Math.Max(0.0f, Math.Min(ChargeSpline.Value.Integrate(from, to), step.Energy(chargePowerLimit ?? float.MaxValue)));

            plant = plant.IntegratedBy(generationEnergy, chargeEnergy, demandEnergy, step, dischargePowerLimit);

            plant.ThrowIfInvalid();

            cost += (plant.LastIntegration.GridCharged + plant.LastIntegration.Shortfall) * unitPrice;
            cost -= (plant.LastIntegration.GridExport) * exportPrice;
            undercharge += plant.LastIntegration.Shortfall;
            overcharge += plant.LastIntegration.Wasted + plant.LastIntegration.GridExport;

            now += step;

            debugResults.Add(new(
                now,
                plant.State.BatteryEnergy, demandEnergy, generationEnergy, chargeEnergy, plant.LastIntegration.GridExport, cost, undercharge, overcharge,
                new(step.Power(generationEnergy), step.Power(chargeEnergy)),
                demandEnergies.Select(f => new IntegrationStepDemandEnergy(f.Profile.Name, f.Profile.Type, (float)f.Energy, step.Power((float)f.Energy))).ToArray()
            ));
        }

        var overchargeAndUnderchargePeriods = debugResults.CalculateOverchargePeriods(timeStep);

        decimal roundedCost;
        try
        {
            roundedCost = Math.Round((decimal)cost, 2);
        }
        catch (OverflowException oe)
        {
            throw new InvalidStateException($"Calculation step resulted in invalid cost of {cost}", oe);
        }

        return new Evaluation(
            chargePowerLimit,
            dischargePowerLimit,
            roundedCost,
            debugResults,
            overchargeAndUnderchargePeriods.Item1,
            overchargeAndUnderchargePeriods.Item2);
    }

    public float DemandEnergyBetween(DateTimeOffset from, DateTimeOffset to)
        => (float)DemandSplines.Value
            .Select(f => (Energy: Math.Max(0.0f, f.Interpolation.Integrate(from, to)), Profile: f.Item2))
            .Select(f => f.Energy)
            .Sum();
}