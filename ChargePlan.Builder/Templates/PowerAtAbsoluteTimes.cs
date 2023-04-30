/// <summary>
/// A data structure that has a list of power values at times of the day.
/// Useful for e.g. baseload demand, or charge profile.
/// </summary>
public record PowerAtAbsoluteTimes(List<(TimeOnly TimeOfDay, float Power)> Values, string? Name = null)
{
    public static PowerAtAbsoluteTimes Empty() => new(new List<(TimeOnly, float)>(), null);

    public ChargeProfile AsChargeProfile(DateTime startAt) => new()
    {
        Values = Values
            .Select(f => new ChargeValue(startAt.Date + f.TimeOfDay.ToTimeSpan(), f.Power))
            .ToList()
    };

    public DemandProfile AsDemandProfile(DateTime startAt) => new()
    {
        Values = Values
            .Select(f => new DemandValue(startAt.Date + f.TimeOfDay.ToTimeSpan(), f.Power))
            .ToList()
    };
}