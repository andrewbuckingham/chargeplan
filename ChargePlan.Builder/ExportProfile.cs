using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record ExportProfile : IExportProfile
{
    public IEnumerable<ExportValue> Values { get; init; } = Enumerable.Empty<ExportValue>();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public ExportProfile Add(ExportProfile other) => new() { Values = this.Values.Concat(other.Values) };
}
