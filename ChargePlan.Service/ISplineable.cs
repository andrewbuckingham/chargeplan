using MathNet.Numerics.Interpolation;

public interface ISplineable
{
    IInterpolation AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator) where T : IInterpolation;
}