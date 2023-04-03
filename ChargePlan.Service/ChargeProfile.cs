using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public class ChargeProfile : ISplineable
{
    public List<ChargeValue> Values = new();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}

public record ChargeValue(DateTime DateTime, float Power);