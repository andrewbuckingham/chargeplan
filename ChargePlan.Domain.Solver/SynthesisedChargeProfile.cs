using ChargePlan.Domain;

namespace ChargePlan.Domain.Solver;

public class SynthesisedChargeProfile : IChargeProfile
{
    public static SynthesisedChargeProfile Empty() => new(new());

    public SynthesisedChargeProfile(List<ChargeValue> values)
    {
        Values = values;
    }

    public List<ChargeValue> Values { get; init; }

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    // public ChargeProfile Add(ChargeProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}