using MathNet.Numerics.Interpolation;

public record PricingProfile : IPricingProfile
{
    public List<PricingValue> Values = new();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public PricingProfile Add(PricingProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}
