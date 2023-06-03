using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record PricingProfile : IPricingProfile
{
    public List<PricingValue> Values = new();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public PricingProfile Add(PricingProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}
