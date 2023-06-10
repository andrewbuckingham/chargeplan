using ChargePlan.Domain;

using System.Collections.Generic;
namespace ChargePlan.Domain.Splines;

public enum InterpolationType
{
    Undefined = 0,
    CubicSpline,
    Step,
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

    private Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> From(InterpolationType type) => type switch
    {
        InterpolationType.CubicSpline => CreateCubicSpline,
        InterpolationType.Step => CreateStepInterpolation,
        _ => throw new InvalidOperationException()
    };

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