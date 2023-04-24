public record Recommendations(
    Evaluation Evaluation,
    IEnumerable<(IShiftableDemandProfile ShiftableDemand, DateTime StartAt, decimal AddedCost)> ShiftableDemands
);