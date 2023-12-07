using ChargePlan.Domain;

namespace ChargePlan.Domain.Solver;

public record SynthesisedPowerProfile(IEnumerable<ChargeValue> Values) : IChargeProfile, IExportProfile
{
    public static SynthesisedPowerProfile Empty() => new(Enumerable.Empty<ChargeValue>());

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    // public ChargeProfile Add(ChargeProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}

public static class ChargeProfileThresholdExtensions
{
    /// <summary>
    /// Create a charge profile that charges whenever the price is below a threshold.
    /// </summary>
    public static IChargeProfile ToChargeProfile(this IInterpolation importPricing, double price, DateTimeOffset start, DateTimeOffset end, float powerWhenCharging)
    {
        // Create a charge profile which charges when the price is less than the threshold.
        List<ChargeValue> chargeValues = new();
        DateTimeOffset instant = start;
        while (instant < end)
        {
            bool shouldPower = importPricing.Interpolate(instant) < price;
            chargeValues.Add(new ChargeValue(instant.DateTime, shouldPower ? powerWhenCharging : 0.0f));
            instant += TimeSpan.FromMinutes(10);
        }

        chargeValues = chargeValues.Take(1).Concat(chargeValues
            .Zip(chargeValues.Skip(1))
            .Where(f => f.First.Power != f.Second.Power)
            .Select(f => f.Second))
            .ToList();

        // Assess how much energy would be charged into the battery from that trial charging profile.
        IChargeProfile trial = new SynthesisedPowerProfile(chargeValues);

        return trial;
    }

    /// <summary>
    /// 
    /// </summary>
    public static IExportProfile ToExportProfile(this IInterpolation exportPricing, double price, DateTimeOffset start, DateTimeOffset end, float powerWhenDischarging)
    {

    }
}