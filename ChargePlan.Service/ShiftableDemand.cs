namespace ChargePlan.Service;

public class ShiftableDemand
{
    public List<ShiftableDemandValue> Values = new();

    public DemandProfile AsDemandProfile(DateTime startingAt)
        => new DemandProfile() { Values = this.Values.Select(f => f.AsDemandValue(startingAt)).ToList() };
}

public record ShiftableDemandValue(TimeSpan RelativeTime, float Power)
{
    public DemandValue AsDemandValue(DateTime startingAt)
        => new DemandValue(startingAt + this.RelativeTime, this.Power);
}