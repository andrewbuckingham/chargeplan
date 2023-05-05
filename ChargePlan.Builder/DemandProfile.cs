using ChargePlan.Domain;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Builder;

public class DemandProfile : IDemandProfile
{
    public List<DemandValue> Values = new();

    public DateTime Starting => Values.Min(f => f.DateTime);

    public DateTime Until => Values.Max(f => f.DateTime);

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    public DemandProfile Add(DemandProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}
