using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public class DemandProfile : ISplineable
{
    public List<DemandValue> Values = new();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}

public record DemandValue(DateTime DateTime, float Power);