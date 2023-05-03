public class ShiftableDemand : IShiftableDemandProfile
{
    public List<ShiftableDemandValue> Values = new();

    public string Name { get; set; } = String.Empty;
    public string Type { get; set; } = String.Empty;

    public TimeOnly Earliest { get; set; } = TimeOnly.MinValue;
    public TimeOnly Latest { get; set; } = TimeOnly.MaxValue;

    public TimeSpan DontRepeatWithin { get; set; } = TimeSpan.FromDays(28);

    public ShiftableDemandPriority Priority { get; set; } = ShiftableDemandPriority.Essential;

    public (DateTime From, DateTime To)? WithinDayRange { get; set; } = null;

    public IDemandProfile AsDemandProfile(DateTime startingAt)
        => new DemandProfile() { Values = this.Values.Select(f => f.AsDemandValue(startingAt)).ToList() };
}

public record ShiftableDemandValue(TimeSpan RelativeTime, float Power)
{
    public DemandValue AsDemandValue(DateTime startingAt)
        => new DemandValue(startingAt + this.RelativeTime, this.Power);
}