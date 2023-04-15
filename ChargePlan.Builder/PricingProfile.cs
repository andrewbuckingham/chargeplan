using MathNet.Numerics.Interpolation;

public class PricingProfile : IPricingProfile
{
    public List<PricingValue> Values = new();

    public DateTime Starting => Values.Min(f => f.DateTime);

    public DateTime Until => Values.Max(f => f.DateTime);

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));
}
