using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record ChargeProfile : IChargeProfile
{
    public static ChargeProfile Empty() => new();

    public IEnumerable<ChargeValue> Values { get; init; } = Enumerable.Empty<ChargeValue>();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    public ChargeProfile Add(ChargeProfile other) => new() { Values = this.Values.Concat(other.Values) };
}