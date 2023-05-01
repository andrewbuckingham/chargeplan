public record ChargePlanAdhocParameters(
    float InitialBatteryEnergy,
    UserPlantParameters Plant,
    List<ShiftableDemandAndPriority> ShiftableDemandsAnyDay,
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
