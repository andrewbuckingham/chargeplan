public record AlgorithmBuilder(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        GenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands)
{
    public AlgorithmBuilder(IPlant plantTemplate)
        : this(plantTemplate, new(), new(), new(), new(), new(), plantTemplate.State, new ShiftableDemand[] {}) {}

    public AlgorithmBuilder WithInitialBatteryEnergy(float kWh)
        => this with { InitialState = InitialState with { BatteryEnergy = kWh } };

    public AlgorithmBuilder WithGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };
    public AlgorithmBuilder WithGeneration(IEnumerable<(DateTime DateTime, float Power)> kwhFigures)
        => this with { GenerationProfile = new() { Values = kwhFigures.Select(f => new GenerationValue(f.DateTime, f.Power)).ToList() } };

    public AlgorithmBuilder AddChargeWindow(PowerAtAbsoluteTimes template, DateTime day) => this with { ChargeProfile = ChargeProfile.Add(template.AsChargeProfile(day.Date)) };
    public AlgorithmBuilder AddDemand(PowerAtAbsoluteTimes template, DateTime day) => this with { DemandProfile = DemandProfile.Add(template.AsDemandProfile(day.Date)) };
    public AlgorithmBuilder AddPricing(PriceAtAbsoluteTimes template, DateTime day) => this with { PricingProfile = PricingProfile.Add(template.AsPricingProfile(day.Date)) };
    public AlgorithmBuilder AddExportPricing(PriceAtAbsoluteTimes template, DateTime day) => this with { ExportProfile = ExportProfile.Add(template.AsExportProfile(day.Date)) };
    public AlgorithmBuilder AddShiftableDemandForDay(PowerAtRelativeTimes template, DateTime day, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, day.Date, day.Date.AddDays(1) )
            }).ToArray() };
    public AlgorithmBuilder AddShiftableDemandAnyDay(PowerAtRelativeTimes template, DateTime? noSoonerThan = null, DateTime? noLaterThan = null, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, noSoonerThan ?? DateTime.Now, noLaterThan ?? DateTime.Now.AddYears(1) )
            }).ToArray() };

    public Algorithm Build() => new Algorithm(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands);
}