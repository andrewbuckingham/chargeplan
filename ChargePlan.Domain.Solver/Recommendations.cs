namespace ChargePlan.Domain.Solver;

public record Recommendations(
    Evaluation Evaluation,
    IEnumerable<ShiftableDemandRecommendation> ShiftableDemands,
    IEnumerable<DemandCompleted> CompletedDemands
);

public record ShiftableDemandRecommendation(
    string Name,
    string Type,
    DateTimeOffset StartAt,
    decimal AddedCost,
    string DemandHash
);
