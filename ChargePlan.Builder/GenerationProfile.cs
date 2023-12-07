using ChargePlan.Domain;

namespace ChargePlan.Builder;

public record GenerationProfile : IGenerationProfile
{
    public IEnumerable<GenerationValue> Values { get; init; } = Enumerable.Empty<GenerationValue>();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}