using ChargePlan.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
namespace ChargePlan.Domain.Splines;

public enum InterpolationType
{
    Undefined = 0,
    CubicSpline,
    Step,
}

file static class InterpolationFactoryCache
{
    public static readonly ConcurrentDictionary<MathNetCacheKey, IInterpolation> Cache = new();
}

public record InterpolationFactory(
    InterpolationType Baseload = InterpolationType.CubicSpline,
    InterpolationType ShiftableDemand = InterpolationType.Step,
    InterpolationType Generation = InterpolationType.CubicSpline,
    InterpolationType Charging = InterpolationType.Step,
    InterpolationType Pricing = InterpolationType.Step,
    InterpolationType Export = InterpolationType.Step
    ) : IInterpolationFactory
{
    private IInterpolation CreateCubicSpline(IEnumerable<double> xValues, IEnumerable<double> yValues)
        => new MathNetWrapper(MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkima(xValues, yValues));

    private IInterpolation CreateStepInterpolation(IEnumerable<double> xValues, IEnumerable<double> yValues)
        => new MathNetWrapper(MathNet.Numerics.Interpolation.StepInterpolation.Interpolate(xValues, yValues));

    private Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> From(InterpolationType type) => (x, y) =>
    {
        var key = MathNetCacheKey.From(x, y, type);
        bool isHit = InterpolationFactoryCache.Cache.ContainsKey(key);

        var result = InterpolationFactoryCache.Cache.GetOrAdd(key, k => type switch
        {
            InterpolationType.CubicSpline => CreateCubicSpline(x, y),
            InterpolationType.Step => CreateStepInterpolation(x, y),
            _ => throw new InvalidOperationException()
        });

        return result;
    };

    // private Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> From(InterpolationType type) => type switch
    // {
    //     InterpolationType.CubicSpline => (x,y) => InterpolationFactoryCache.Cache.GetOrAdd(MathNetCacheKey.From(x, y, type), k => CreateCubicSpline(x, y)),
    //     InterpolationType.Step => (x,y) => InterpolationFactoryCache.Cache.GetOrAdd(MathNetCacheKey.From(x, y, type), k => CreateStepInterpolation(x, y)),
    //     _ => throw new InvalidOperationException()
    // };

    public IInterpolation InterpolateBaseload(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(Baseload)(xValues, yValues);
    public IInterpolation InterpolateShiftableDemand(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(ShiftableDemand)(xValues, yValues);
    public IInterpolation InterpolateGeneration(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(Generation)(xValues, yValues);
    public IInterpolation InterpolateCharging(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(Charging)(xValues, yValues);
    public IInterpolation InterpolatePricing(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(Pricing)(xValues, yValues);
    public IInterpolation InterpolateExport(IEnumerable<double> xValues, IEnumerable<double> yValues) => From(Export)(xValues, yValues);
}

file record MathNetWrapper(MathNet.Numerics.Interpolation.IInterpolation Wrapped) : IInterpolation
{
    public double Integrate(double a, double b) => Wrapped.Integrate(a, b);
    public double Interpolate(double t) => Wrapped.Interpolate(t);
}

file record MathNetCacheKey(int hashCode, InterpolationType type)
{
    public static MathNetCacheKey From(IEnumerable<double> xValues, IEnumerable<double> yValues, InterpolationType type)
    {
        unchecked
        {
            return new(
                xValues.Concat(yValues).Aggregate(23, (acc, value) => acc * 31 + value.GetHashCode()),
                type
            );
        }
    }
}