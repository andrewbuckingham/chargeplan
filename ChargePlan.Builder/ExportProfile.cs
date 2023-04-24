using MathNet.Numerics.Interpolation;

public class ExportProfile : IExportProfile
{
    public List<ExportValue> Values = new();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.PricePerUnit));

    public ExportProfile Add(ExportProfile other) => new() { Values = new(this.Values.Concat(other.Values)) };
}
