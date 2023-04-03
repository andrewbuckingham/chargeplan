using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public class PricingProfile : ISplineable
{
    public List<PricingValue> Values = new();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));
}

public record PricingValue(DateTime DateTime, decimal PricePerUnit);