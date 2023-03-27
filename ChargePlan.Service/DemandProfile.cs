namespace ChargePlan.Service;

public class DemandProfile
{
    public List<DemandValue> Values = new();
}

public record DemandValue(DateTime DateTime, float Power);