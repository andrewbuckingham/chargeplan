using ChargePlan.Domain;

namespace ChargePlan.Builder;

public class ExportProfile : IExportProfile
{
    public List<ExportValue> Values = new();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public ExportProfile Add(ExportProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}
