namespace ChargePlan.Service;

public class ChargeProfile
{
    public List<ChargeValue> Values = new();
}

public record ChargeValue(DateTime DateTime, float Power);