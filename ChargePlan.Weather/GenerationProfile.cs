using MathNet.Numerics.Interpolation;

public class GenerationProfile : IGenerationProfile
{
    public IEnumerable<GenerationValue> Values = Enumerable.Empty<GenerationValue>();

    public IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}