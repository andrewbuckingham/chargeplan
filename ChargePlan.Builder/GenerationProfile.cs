using ChargePlan.Domain;

namespace ChargePlan.Builder;

public class GenerationProfile : IGenerationProfile
{
    public List<GenerationValue> Values = new();

    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}