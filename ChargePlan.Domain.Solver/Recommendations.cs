public record Recommendations(
    Evaluation Evaluation,
    IEnumerable<ShiftableDemandRecommendation> ShiftableDemands
);

public record ShiftableDemandRecommendation(
    IShiftableDemandProfile ShiftableDemand,
    DateTime StartAt,
    decimal AddedCost
);