using MathNet.Numerics.Interpolation;

public class GenerationProfile : IGenerationProfile
{
    public IEnumerable<GenerationValue> Values = Enumerable.Empty<GenerationValue>();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));

    public override string ToString()
        => String.Join(" | ", Values.Zip(Values.Skip(1)).Select(f => $"{(f.First.DateTime.Date != f.Second.DateTime.Date ? f.Second.DateTime : f.Second.DateTime.TimeOfDay)}h:{Math.Round(f.Second.Power, 3)}"));
}