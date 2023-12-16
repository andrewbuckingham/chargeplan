using ChargePlan.Domain;

namespace ChargePlan.Domain.Solver;

public record SynthesisedPowerProfile(IEnumerable<ChargeValue> Values) : IChargeProfile
{
    public static SynthesisedPowerProfile Empty() => new(Enumerable.Empty<ChargeValue>());

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    // public ChargeProfile Add(ChargeProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}

public record SynthesisedDemandProfile(IEnumerable<ChargeValue> Values, DateTime Starting, DateTime Until, string Name, string Type)
    : SynthesisedPowerProfile(Values), IDemandProfile
{
}

public static class ChargeProfileThresholdExtensions
{
    /// <summary>
    /// Create a charge profile that charges whenever the price is below a threshold.
    /// </summary>
    public static IChargeProfile ToChargeProfile(this IInterpolation importPricing, double whenPriceIsBelow, DateTimeOffset start, DateTimeOffset end, float powerWhenCharging)
    {
        // Create a charge profile which charges when the price is less than the threshold.
        List<ChargeValue> chargeValues = new();
        DateTimeOffset instant = start;
        while (instant < end)
        {
            bool shouldPower = importPricing.Interpolate(instant) < whenPriceIsBelow;
            chargeValues.Add(new ChargeValue(instant.DateTime, shouldPower ? powerWhenCharging : 0.0f));
            instant += TimeSpan.FromMinutes(10);
        }

        chargeValues = chargeValues.Take(1).Concat(chargeValues
            .Zip(chargeValues.Skip(1))
            .Where(f => f.First.Power != f.Second.Power)
            .Select(f => f.Second))
            .ToList();

        IChargeProfile profile = new SynthesisedPowerProfile(chargeValues);

        return profile;
    }

    /// <summary>
    /// Create a force export demand that exports whenever the price is above a threshold.
    /// </summary>
    public static IDemandProfile ToForceExportProfile(this IInterpolation exportPricing, double whenPriceIsAbove, DateTimeOffset start, DateTimeOffset end, float powerWhenDischarging)
    {
        List<ChargeValue> chargeValues = new();
        DateTimeOffset instant = start;
        while (instant < end)
        {
            bool shouldPower = exportPricing.Interpolate(instant) > whenPriceIsAbove;
            chargeValues.Add(new ChargeValue(instant.DateTime, shouldPower ? powerWhenDischarging : 0.0f));
            instant += TimeSpan.FromMinutes(10);
        }

        chargeValues = chargeValues.Take(1).Concat(chargeValues
            .Zip(chargeValues.Skip(1))
            .Where(f => f.First.Power != f.Second.Power)
            .Select(f => f.Second))
            .ToList();

        IDemandProfile profile = new SynthesisedDemandProfile(chargeValues, start.LocalDateTime, end.LocalDateTime, Calculator.ForceExportDemandName, Calculator.ForceExportDemandName);

        return profile;
    }
}