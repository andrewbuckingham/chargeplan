public record AlgorithmBuilderForPeriod(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        IGenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        ExportProfile ExportProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands,
        DateTime? ExplicitStartDate,
        params DateTime[] Days)
{
    public AlgorithmBuilderForPeriod AddChargeWindow(PowerAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { ChargeProfile = builder.ChargeProfile.Add(template.AsChargeProfile(day.Date)) });
    public AlgorithmBuilderForPeriod AddDemand(PowerAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { DemandProfile = builder.DemandProfile.Add(template.AsDemandProfile(day.Date)) });
    public AlgorithmBuilderForPeriod AddPricing(PriceAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { PricingProfile = builder.PricingProfile.Add(template.AsPricingProfile(day.Date)) });
    public AlgorithmBuilderForPeriod AddExportPricing(PriceAtAbsoluteTimes template) => AddForEachDay((builder, day) => builder with { ExportProfile = builder.ExportProfile.Add(template.AsExportProfile(day.Date)) });

    /// <summary>
    /// Add a demand which needs to be run at some point on this day/s, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest and latest permissable times of day.
    /// </summary>
    public AlgorithmBuilderForPeriod AddShiftableDemand(PowerAtRelativeTimes template, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
        => AddForEachDay((builder, day) => builder with { ShiftableDemands = builder.ShiftableDemands.Concat(new[] {
            template.AsShiftableDemand(priority, (day.Date, day.Date.AddDays(1)))
            }).ToArray() });

    /// <summary>
    /// Add a demand which needs to be run at some point on this day/s, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest permissable time of day.
    /// </summary>
    // public AlgorithmBuilderForPeriod AddShiftableDemand(PowerAtRelativeTimes template, TimeOnly noSoonerThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
    //     => AddForEachDay((builder, day) => builder with { ShiftableDemands = builder.ShiftableDemands.Concat(new[] {
    //         template.AsShiftableDemand(priority, (day.Date.Add(noSoonerThan.ToTimeSpan()), day.Date.AddDays(1)))
    //         }).ToArray() });

    /// <summary>
    /// Add a demand which needs to be run at some point on this day/s, and the algorithm will determine the optimum day and time to run it.
    /// Allows to specify the earliest and latest permissable times of day.
    /// </summary>
    // public AlgorithmBuilderForPeriod AddShiftableDemand(PowerAtRelativeTimes template, TimeOnly noSoonerThan, TimeOnly noLaterThan, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential)
    //     => AddForEachDay((builder, day) => builder with { ShiftableDemands = builder.ShiftableDemands.Concat(new[] {
    //         template.AsShiftableDemand(priority, (day.Date.Add(noSoonerThan.ToTimeSpan()), day.Date.Add(noLaterThan.ToTimeSpan())))
    //         }).ToArray() });

    /// <summary>
    /// Any further builder instructions will be for the supplied day or days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForDay(DateTime day) => ForEachDay(new DateTime[] { day });

    /// <summary>
    /// Any further builder instructions will be for the supplied days. Existing ones are preserved.
    /// </summary>
    public AlgorithmBuilderForPeriod ForEachDay(params DateTime[] days)
        => new(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands, ExplicitStartDate, days);

    private AlgorithmBuilderForPeriod AddForEachDay(Func<AlgorithmBuilderForPeriod, DateTime, AlgorithmBuilderForPeriod> action)
    {
        var builder = this;
        foreach (DateTime day in Days)
        {
            builder = action(builder, day);
        }
        return builder;
    }

    public Algorithm Build() => new Algorithm(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, ExportProfile, InitialState, ShiftableDemands, ExplicitStartDate);
}