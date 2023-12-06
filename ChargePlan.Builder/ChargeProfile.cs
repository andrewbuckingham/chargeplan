using ChargePlan.Domain;

namespace ChargePlan.Builder;

public class ChargeProfile : IChargeProfile
{
    public static ChargeProfile Empty() => new();

    public List<ChargeValue> Values { get; init; } = new();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    public ChargeProfile Add(ChargeProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}