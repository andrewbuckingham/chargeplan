public record ChargePlanTemplatedParameters(
    List<DayTemplate> DayTemplates,
    List<ShiftableDemandNameAndPriority> ShiftableDemandsAnyDay
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
    ShiftableDemandPriority Priority
);
