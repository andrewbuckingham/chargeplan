using ChargePlan.Builder.Templates;
using ChargePlan.Domain;
using ChargePlan.Domain.Solver;

namespace ChargePlan.Builder;

public record AlgorithmBuilder(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        IGenerationProfile GenerationProfile,
        ChargeProfile? FixedChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        IInterpolationFactory InterpolationFactory,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands,
        DemandCompleted[] CompletedDemands,
        DateTime? ExplicitStartDate,
        AlgorithmPrecision AlgorithmPrecision)
{
    public AlgorithmBuilder(IPlant plantTemplate, IInterpolationFactory interpolationFactory) : this(
        plantTemplate, new(), new GenerationProfile(), ChargeProfile.Empty(), new(), new(), interpolationFactory, plantTemplate.State,
        new ShiftableDemand[] {}, new DemandCompleted[] {}, null,
        AlgorithmPrecision.Default// with { TimeStep = TimeSpan.FromHours(1), ShiftBy = TimeSpan.FromHours(4) }
        ) {}

    /// <summary>
    /// Set how much energy is in the battery storage at the start of the period.
    /// Default is zero.
    /// </summary>
    public AlgorithmBuilder WithInitialBatteryEnergy(float kWh)
        => this with { InitialState = InitialState with { BatteryEnergy = kWh } };

    /// <summary>
    /// Set an explicit start date for the modelling.
    /// Default is Now.
    /// </summary>
    public AlgorithmBuilder WithExplicitStartDate(DateTime datum)
        => this with { ExplicitStartDate = datum };

    /// <summary>
    /// Specify solar generation.
    /// </summary>
    public AlgorithmBuilder WithGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new GenerationProfile() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };
    public AlgorithmBuilder WithGeneration(IEnumerable<(DateTime DateTime, float Power)> kwhFigures)
        => this with { GenerationProfile = new GenerationProfile() { Values = kwhFigures.Select(f => new GenerationValue(f.DateTime, f.Power)).ToList() } };
    public AlgorithmBuilder WithGeneration(IGenerationProfile generationProfile)
        => this with { GenerationProfile = generationProfile };

    /// <summary>
    /// Explicitly no grid charge periods.
    /// </summary>
    public AlgorithmBuilder WithoutChargeWindows()
        => this with { FixedChargeProfile = ChargeProfile.Empty() };

    /// <summary>
    /// Dynamically create the charge windows based on the loads.
    public AlgorithmBuilder WithDynamicChargeWindows(Func<DynamicChargeWindowCalculations, DynamicChargeWindowCalculations> mutator)
        => this with
        {
            FixedChargeProfile = null,
            AlgorithmPrecision = this.AlgorithmPrecision with
            {
                AutoChargeWindow = mutator(this.AlgorithmPrecision.AutoChargeWindow)
            }
        };
    public AlgorithmBuilder WithDynamicChargeWindows()
        => this with { FixedChargeProfile = null };

    public AlgorithmBuilder WithPrecision(AlgorithmPrecision precision)
        => this with { AlgorithmPrecision = precision };
    public AlgorithmBuilder WithPrecision(Func<AlgorithmPrecision, AlgorithmPrecision> mutator)
        => this with { AlgorithmPrecision = mutator(AlgorithmPrecision) };

    /// <summary>
    /// Add a demand which needs to be run at some point on any day, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest and latest permissable times of day.
    /// </summary>
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime? noSoonerThan = null, DateTime? noLaterThan = null, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential, TimeSpan? dontRepeatWithin = null)
        => this with { ShiftableDemands = ShiftableDemands
            .Append(template.AsShiftableDemand(
                priority,
                noSoonerThan != null && noLaterThan != null ? (noSoonerThan ?? DateTime.Today.AddYears(-1), noLaterThan ?? DateTime.Today.AddYears(1)) : null,
                dontRepeatWithin))
            .ToArray() };

    /// <summary>
    /// If demands have already been completed, then exclude them from the calculations.
    /// </summary>
    public AlgorithmBuilder ExcludingCompletedDemands(IEnumerable<DemandCompleted> completedDemands)
        => this with { CompletedDemands = CompletedDemands.Concat(completedDemands).ToArray() };

    /// <summary>
    /// Any further builder instructions will be for the supplied day. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForDay(DateTime day) => ForEachDay(new DateTime[] { day });

    /// <summary>
    /// Any further builder instructions will be for the supplied days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForEachDay(params DateTime[] days)
        => new(PlantTemplate, DemandProfile, GenerationProfile, FixedChargeProfile, PricingProfile, ExportProfile, InterpolationFactory, InitialState, ShiftableDemands, CompletedDemands, ExplicitStartDate, AlgorithmPrecision, days);
}