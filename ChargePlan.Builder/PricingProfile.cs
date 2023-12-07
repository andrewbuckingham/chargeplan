using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record PricingProfile : IPricingProfile
{
    public IEnumerable<PricingValue> Values { get; init; } = Enumerable.Empty<PricingValue>();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public PricingProfile Add(PricingProfile other) => new() { Values = this.Values.Concat(other.Values) };
}
