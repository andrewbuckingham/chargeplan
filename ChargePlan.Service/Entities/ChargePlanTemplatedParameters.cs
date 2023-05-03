public record ChargePlanTemplatedParameters(
    List<DayTemplate> DayTemplates,
    List<ShiftableDemandNameAndPriorityOverDays> ShiftableDemandsAnyDay
);

public record DayTemplate(
    List<ShiftableDemandNameAndPriority> ShiftableDemands,
    DayOfWeek DayOfWeek = DayOfWeek.Monday,
    string DemandName = "",
    string ChargeName = "",
    string PricingName = "",
    string ExportName = ""
);

public record ShiftableDemandNameAndPriority(
    string Name,
    ShiftableDemandPriority Priority = ShiftableDemandPriority.Essential,
    TimeSpan? DontRepeatWithin = null,
    bool Disabled = false
);

public record ShiftableDemandNameAndPriorityOverDays(
    string Name,
    int OverNumberOfDays,
    ShiftableDemandPriority Priority = ShiftableDemandPriority.High,
    TimeSpan? DontRepeatWithin = null,
    bool Disabled = false
)
{
    public IEnumerable<DateTime> ApplicableDatesStartingFrom(DateTime datum)
        => Enumerable.Range(0, OverNumberOfDays).Select(f => datum.Date.AddDays(f));
}
