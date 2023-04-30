public record AlgorithmBuilder(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        IGenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands,
        DateTime? ExplicitStartDate)
{
    public AlgorithmBuilder(IPlant plantTemplate)
        : this(plantTemplate, new(), new GenerationProfile(), new(), new(), new(), plantTemplate.State, new ShiftableDemand[] {}, null) {}

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
    /// Add a demand which needs to be run at some point on any day, and the algorithm will determine the optimum day and time to run it.
    /// </summary>
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, null)
            }).ToArray() };

    /// <summary>
    /// Add a demand which needs to be run at some point on any day, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest permissable time of day.
    /// </summary>
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime noSoonerThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (noSoonerThan, noSoonerThan.Date.AddYears(1)))
            }).ToArray() };

    /// <summary>
    /// Add a demand which needs to be run at some point on any day, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest and latest permissable times of day.
    /// </summary>
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime noSoonerThan, DateTime noLaterThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (noSoonerThan, noLaterThan))
            }).ToArray() };

    /// <summary>
    /// Any further builder instructions will be for the supplied day. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForDay(DateTime day) => ForEachDay(new DateTime[] { day });

    /// <summary>
    /// Any further builder instructions will be for the supplied days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForEachDay(params DateTime[] days)
        => new(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands, ExplicitStartDate, days);
}