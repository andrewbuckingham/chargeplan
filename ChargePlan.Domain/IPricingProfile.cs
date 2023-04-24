public interface IPricingProfile : ISplineable<PricingValue>
{
}

public record PricingValue(DateTime DateTime, decimal PricePerUnit);