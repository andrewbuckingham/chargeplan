public record AlgorithmBuilderForDay(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        GenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands,
        params DateTime[] Days)
{
    public AlgorithmBuilderForDay AddChargeWindow(PowerAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { ChargeProfile = builder.ChargeProfile.Add(template.AsChargeProfile(day.Date)) });
    public AlgorithmBuilderForDay AddDemand(PowerAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { DemandProfile = builder.DemandProfile.Add(template.AsDemandProfile(day.Date)) });
    public AlgorithmBuilderForDay AddPricing(PriceAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { PricingProfile = builder.PricingProfile.Add(template.AsPricingProfile(day.Date)) });
    public AlgorithmBuilderForDay AddExportPricing(PriceAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { ExportProfile = builder.ExportProfile.Add(template.AsExportProfile(day.Date)) });
    public AlgorithmBuilderForDay AddShiftableDemand(PowerAtRelativeTimes template, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => AddForEachDay((builder, day) => builder with { ShiftableDemands = builder.ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, day.Date, day.Date.AddDays(1) )
            }).ToArray() });

    /// <summary>
    /// Any futher builder instructions will be for the supplied day or days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForDay For(params DateTime[] days) => this with { Days = days };

    private AlgorithmBuilderForDay AddForEachDay(Func<AlgorithmBuilderForDay, DateTime, AlgorithmBuilderForDay> action)
    {
        var builder = this;
        foreach (DateTime day in Days)
        {
            builder = action(builder, day);
        }
        return builder;
    }

    public Algorithm Build() => new Algorithm(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands);
}