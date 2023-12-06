using ChargePlan.Domain;

namespace ChargePlan.Domain.Solver;

public class SynthesisedPowerProfile : IChargeProfile
{
    public static SynthesisedPowerProfile Empty() => new(new());

    public SynthesisedPowerProfile(List<ChargeValue> values)
    {
        Values = values;
    }

    public List<ChargeValue> Values { get; init; }

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    // public ChargeProfile Add(ChargeProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}

public static class ChargeProfileThresholdExtensions
{
    public static IChargeProfile WhenPriceIsBelow(IInterpolation pricing, double price, DateTimeOffset start, DateTimeOffset end)
    {
        // Create a charge profile which charges when the price is less than the threshold.
        List<ChargeValue> chargeValues = new();
        DateTimeOffset instant = start;
        while (instant < end)
        {
            bool shouldPower = pricing.Interpolate(instant) < price;
            chargeValues.Add(new ChargeValue(instant.DateTime, shouldPower ? PlantTemplate.ChargeRateAtScalar(1.0f) : 0.0f));
            instant += stepOutput;
        }

        chargeValues = chargeValues.Take(1).Concat(chargeValues
            .Zip(chargeValues.Skip(1))
            .Where(f => f.First.Power != f.Second.Power)
            .Select(f => f.Second))
            .ToList();

        // Assess how much energy would be charged into the battery from that trial charging profile.
        IChargeProfile trial = new SynthesisedPowerProfile(chargeValues);
    }
}