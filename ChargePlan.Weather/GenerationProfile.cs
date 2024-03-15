using ChargePlan.Domain;

namespace ChargePlan.Weather;

public record GenerationProfile(IEnumerable<DniValue> DniValues, IEnumerable<DateTimeOffset> Clock, Func<DateTimeOffset, DniValue, GenerationValue> Algorithm) : IGenerationProfile
{
    public IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
    {
        IInterpolation directWatts = splineCreator(DniValues.Select(f => f.DateTime.AsTotalHours()), DniValues.Select(f => (double)f.DirectWatts));
        IInterpolation diffusewatts = splineCreator(DniValues.Select(f => f.DateTime.AsTotalHours()), DniValues.Select(f => (double)(f.DiffuseWatts ?? 0.0f)));
        IInterpolation cloud = splineCreator(DniValues.Select(f => f.DateTime.AsTotalHours()), DniValues.Select(f => (double)(f.CloudCoverPercent ?? 0.0f)));

        var atEachPointInTime = Clock.Select(f =>
        {
            DniValue interpolatedDni = new(
                DateTime: f,
                DirectWatts: (float)directWatts.Interpolate(f.AsTotalHours()),
                DiffuseWatts: (float)diffusewatts.Interpolate(f.AsTotalHours()),
                CloudCoverPercent: (int)cloud.Interpolate(f.AsTotalHours())
            );

            var value = Algorithm(f, interpolatedDni);

            return value;
        }).ToArray();

        IInterpolation final = splineCreator(
            atEachPointInTime.Select(f => f.DateTime.AsTotalHours()),
            atEachPointInTime.Select(f => (double)f.Power)
            );

        return final;
    }
}