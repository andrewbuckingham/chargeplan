public interface ISplineable
{
    T AsSpline<T>(Func<IEnumerable<double>, IEnumerable<double>, T> splineCreator);
}