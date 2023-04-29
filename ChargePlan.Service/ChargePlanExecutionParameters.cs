public record ChargePlanExecutionParameters(
    float InitialBatteryEnergy,
    ArraySpecification ArraySpecification,
    List<ShiftableDemandAndPriority> ShiftableDemandAnyDay,
    List<DaySpecification> Days
);

public record ArraySpecification(
    float ArrayArea = 0.0f,
    float ArrayElevationDegrees = 45.0f, float ArrayAzimuthDegrees = 0.0f,
    float LatDegrees = 54.5f, float LongDegrees = -1.55f
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
