/// <summary>
/// A data structure that has a list of powers at times relative to each other.
/// Useful for e.g. a shiftable demand load such as a washing machine, where there's no absolute time of day.
/// </summary>
public record PowerAtRelativeTimes(List<(TimeSpan RelativeTime, float Power)> Values, string Name, TimeOnly? Earliest = null, TimeOnly? Latest = null)
{
    public static PowerAtRelativeTimes Empty() => new(new(), String.Empty, null, null);

    public ShiftableDemand AsShiftableDemand(ShiftableDemandPriority priority, DateTime NoSoonerThan, DateTime NoLaterThan) => new()
    {
        Values = Values
            .Select(f => new ShiftableDemandValue(f.RelativeTime, f.Power))
            .ToList(),
        Earliest = Earliest ?? TimeOnly.MinValue,
        Latest = Latest ?? TimeOnly.MaxValue,
        Name = Name,
        Priority = priority,
        WithinDayRange = (NoSoonerThan, NoLaterThan)
    };
}
