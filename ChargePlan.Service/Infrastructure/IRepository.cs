public interface IRepository<T>
{
    Task<T?> GetAsync(Guid userId);
    Task<T> UpsertAsync(Guid userId, T entity);
}

public static class IRepositoryExtensions
{
    public static async Task<IEnumerable<T>> GetAsyncOrEmpty<T>(this IRepository<IEnumerable<T>> repo, Guid userId)
        => (await repo.GetAsync(userId)) ?? Enumerable.Empty<T>();
}