public record ChargePlanAdhocParameters(
    float InitialBatteryEnergy,
    ArraySpecification ArraySpecification,
    List<ShiftableDemandAndPriority> ShiftableDemandAnyDay,
    List<DaySpecification> Days
);

public record DaySpecification(
    List<DateTime> Dates,
    PowerAtAbsoluteTimes Demand,
    PowerAtAbsoluteTimes Charge,
    PriceAtAbsoluteTimes Pricing,
    PriceAtAbsoluteTimes Export,
    List<ShiftableDemandAndPriority> ShiftableDemands
);

public record ShiftableDemandAndPriority(
    PowerAtRelativeTimes PowerAtRelativeTimes,
    ShiftableDemandPriority Priority
);
