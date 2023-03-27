namespace ChargePlan.Service;

public class PricingProfile : ISplineable
{
    public List<PricingValue> Values = new();

    public T AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));
}

public record PricingValue(DateTime DateTime, decimal PricePerUnit);