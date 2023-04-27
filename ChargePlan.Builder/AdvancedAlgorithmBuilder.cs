/// <summary>
/// An algorithm builder that can build any number of days.
/// </summary>
/// <param name="PlantTemplate"></param>
/// <param name="DemandProfile"></param>
/// <param name="GenerationProfile"></param>
/// <param name="ChargeProfile"></param>
/// <param name="PricingProfile"></param>
/// <param name="ExportProfile"></param>
/// <param name="InitialState"></param>
/// <param name="ShiftableDemands"></param>
public record AdvancedAlgorithmBuilder(IPlant PlantTemplate,
    DemandProfile DemandProfile,
    IGenerationProfile GenerationProfile,
    ChargeProfile ChargeProfile,
    PricingProfile PricingProfile,
    ExportProfile ExportProfile,
    PlantState InitialState,
    ShiftableDemand[] ShiftableDemands) : AlgorithmBuilder(PlantTemplate,
        DemandProfile,
        GenerationProfile,
        ChargeProfile,
        PricingProfile,
        ExportProfile,
        InitialState,
        ShiftableDemands)
{
    public AdvancedAlgorithmBuilder AddChargeWindow(PowerAtAbsoluteTimes template, DateTime day) => this with { ChargeProfile = ChargeProfile.Add(template.AsChargeProfile(day.Date)) };
    public AdvancedAlgorithmBuilder AddDemand(PowerAtAbsoluteTimes template, DateTime day) => this with { DemandProfile = DemandProfile.Add(template.AsDemandProfile(day.Date)) };
    public AdvancedAlgorithmBuilder AddPricing(PriceAtAbsoluteTimes template, DateTime day) => this with { PricingProfile = PricingProfile.Add(template.AsPricingProfile(day.Date)) };
    public AdvancedAlgorithmBuilder AddExportPricing(PriceAtAbsoluteTimes template, DateTime day) => this with { ExportProfile = ExportProfile.Add(template.AsExportProfile(day.Date)) };
    public AdvancedAlgorithmBuilder AddShiftableDemand(PowerAtRelativeTimes template, DateTime day, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => this with { ShiftableDemands = ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (day.Date, day.Date.AddDays(1)))
            }).ToArray() };
}