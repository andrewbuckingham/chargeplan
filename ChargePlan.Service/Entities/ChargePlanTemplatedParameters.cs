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
    bool Disabled = false
);

public record ShiftableDemandNameAndPriorityOverDays(
    string Name,
    int OverNumberOfDays,
    ShiftableDemandPriority Priority = ShiftableDemandPriority.High,
    bool Disabled = false
);
