using MathNet.Numerics.Interpolation;

public interface ISplineable<TValue>
{
    IInterpolation AsSpline<TSpline>(Func<IEnumerable<double>, IEnumerable<double>, TSpline> splineCreator) where TSpline : IInterpolation;
}