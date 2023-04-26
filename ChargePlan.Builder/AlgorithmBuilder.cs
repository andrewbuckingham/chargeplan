public record AlgorithmBuilder(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        IGenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands)
{
    public AlgorithmBuilder(IPlant plantTemplate)
        : this(plantTemplate, new(), new GenerationProfile(), new(), new(), new(), plantTemplate.State, new ShiftableDemand[] {}) {}

    public AlgorithmBuilder WithInitialBatteryEnergy(float kWh)
        => this with { InitialState = InitialState with { BatteryEnergy = kWh } };

    public AlgorithmBuilder WithGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new GenerationProfile() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };
    public AlgorithmBuilder WithGeneration(IEnumerable<(DateTime DateTime, float Power)> kwhFigures)
        => this with { GenerationProfile = new GenerationProfile() { Values = kwhFigures.Select(f => new GenerationValue(f.DateTime, f.Power)).ToList() } };
    public AlgorithmBuilder WithGeneration(IGenerationProfile generationProfile)
        => this with { GenerationProfile = generationProfile };

    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, null)
            }).ToArray() };
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime noSoonerThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (noSoonerThan, noSoonerThan.Date.AddYears(1)))
            }).ToArray() };
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime noSoonerThan, DateTime noLaterThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (noSoonerThan, noLaterThan))
            }).ToArray() };

    /// <summary>
    /// Any futher builder instructions will be for the supplied day or days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForDay(DateTime day) => ForEachDay(new DateTime[] { day });

    public AlgorithmBuilderForPeriod ForEachDay(params DateTime[] days)
        => new(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands, days);
}