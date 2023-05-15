namespace ChargePlan.Domain.Solver;

public record Recommendations(
    Evaluation Evaluation,
    IEnumerable<ShiftableDemandRecommendation> ShiftableDemands
);

public record ShiftableDemandRecommendation(
    string Name,
    string Type,
    DateTime StartAt,
    decimal AddedCost,
    string DemandHash
);