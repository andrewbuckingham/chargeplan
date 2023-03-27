namespace ChargePlan.Service;

public class PricingProfile
{
    public List<PricingValue> Values = new();
}

public record PricingValue(DateTime DateTime, decimal PricePerUnit);