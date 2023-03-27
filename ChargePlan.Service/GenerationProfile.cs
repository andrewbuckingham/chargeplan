namespace ChargePlan.Service;

public record GenerationProfile : ISplineable
{
    public List<GenerationValue> Values = new();

    public T AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator)
        => splineCreator(Values.Select(f => (double)f.DateTime.AsTotalHours()), Values.Select(f => (double)f.Power));
}

public record GenerationValue(DateTime DateTime, float Power);