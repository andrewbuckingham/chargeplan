using MathNet.Numerics.Interpolation;

namespace ChargePlan.Domain;

public interface ISplineable<TValue>
{
    IInterpolation AsSpline(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator);

    /// <summary>
    /// If this data structure doesn't have any points specified, and so can't be a spline, then return
    /// a dummy function that yields zero for any value.
    /// </summary>
    IInterpolation AsSplineOrZero(Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> splineCreator)
    {
        Func<IEnumerable<double>, IEnumerable<double>, IInterpolation> wrapper = (x, y) => x.Any()
            ? splineCreator(x, y)
            : new DummySpline();

        return AsSpline(wrapper);
    }
}

public class DummySpline : IInterpolation
{
    public bool SupportsDifferentiation => false;

    public bool SupportsIntegration => true;

    public double Differentiate(double t) => throw new NotImplementedException();
    public double Differentiate2(double t) => throw new NotImplementedException();
    public double Integrate(double t) => throw new NotImplementedException();

    public double Integrate(double a, double b) => 0.0;
    public double Interpolate(double t) => 0.0;
}
