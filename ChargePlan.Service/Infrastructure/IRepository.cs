namespace ChargePlan.Service.Infrastructure;

public interface IRepository<T>
{
    Task<T?> GetAsync(Guid userId);
    Task<T> UpsertAsync(Guid userId, T entity);
}

public record EtaggedEntity<T>(T Entity, string ETag);

public interface IEtaggedRepository<T>
{
    Task<EtaggedEntity<T>?> GetAsync(Guid userId);
    Task<EtaggedEntity<T>> UpsertAsync(Guid userId, EtaggedEntity<T> entity);
}

public static class IRepositoryExtensions
{
    public static async Task<IEnumerable<T>> GetAsyncOrEmpty<T>(this IRepository<IEnumerable<T>> repo, Guid userId)
        => (await repo.GetAsync(userId)) ?? Enumerable.Empty<T>();

    public static async Task<EtaggedEntity<T[]>> GetAsyncOrEmpty<T>(this IEtaggedRepository<T[]> repo, Guid userId)
        => (await repo.GetAsync(userId)) ?? new EtaggedEntity<T[]>(new T[] {}, String.Empty);
}