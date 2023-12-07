using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record DemandProfile : IDemandProfile
{
    public IEnumerable<DemandValue> Values { get; init; } = Enumerable.Empty<DemandValue>();

    public DateTime Starting => Values.Min(f => f.DateTime.ToLocalTime());

    public DateTime Until => Values.Max(f => f.DateTime.ToLocalTime());

    public string Name { get; set; } = String.Empty;
    public string Type { get; set; } = String.Empty;

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    public DemandProfile Add(DemandProfile other) => new() { Values = this.Values.Concat(other.Values) };
}
