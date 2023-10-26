namespace ChargePlan.Service.Infrastructure;
using ChargePlan.Domain;

public interface IRepository<T>
{
    Task<T?> GetAsync(Guid userId);
    Task<T> UpsertAsync(Guid userId, T entity);
}

public record EtaggedEntity<T>(T Entity, string ETag);
public record EtaggedEntityWithId<TEntity>(TEntity Entity, string ETag, string Id);

public interface IEtaggedRepository<T>
{
    Task<EtaggedEntity<T>?> GetAsync(Guid userId);
    Task<EtaggedEntity<T>> UpsertAsync(Guid userId, EtaggedEntity<T> entity);
}

public interface IEtaggedRepositoryWithId<T>
{
    Task<EtaggedEntityWithId<T>?> GetAsync(Guid userId, string id);
    Task<EtaggedEntityWithId<T>> UpsertAsync(Guid userId, EtaggedEntityWithId<T> entity);
}

public interface IEtaggedRepositoryByMonth<T> : IEtaggedRepositoryWithId<T> where T : class, new()
{
    Task<EtaggedEntityWithId<T>?> GetAsync(Guid userId, DateTimeOffset month)
        => this.GetAsync(userId, month.ToStartOfMonth().ToString("yyyy-MM-dd"));

    EtaggedEntityWithId<T> Create(Guid userId, DateTimeOffset month)
        => new(new(), String.Empty, month.ToStartOfMonth().ToString("yyyy-MM-dd"));

    async Task<IEnumerable<T>> GetSinceAsync(Guid userId, DateTimeOffset earliestMonth)
    {
        List<T> list = new();
        earliestMonth = earliestMonth.ToStartOfMonth();
        while (earliestMonth <= DateTimeOffset.Now.ToStartOfMonth())
        {
            var monthData = (await GetAsync(userId, earliestMonth))?.Entity;
            if (monthData != null) list.Add(monthData);

            earliestMonth = earliestMonth.AddMonths(1);
        }

        return list;
    }
}

public static class IRepositoryExtensions
{
    public static async Task<IEnumerable<T>> GetAsyncOrEmpty<T>(this IRepository<IEnumerable<T>> repo, Guid userId)
        => (await repo.GetAsync(userId)) ?? Enumerable.Empty<T>();

    public static async Task<EtaggedEntity<T[]>> GetAsyncOrEmpty<T>(this IEtaggedRepository<T[]> repo, Guid userId)
        => (await repo.GetAsync(userId)) ?? new EtaggedEntity<T[]>(Array.Empty<T>(), String.Empty);

    public static async Task<EtaggedEntityWithId<T[]>> GetAsyncOrEmpty<T>(this IEtaggedRepositoryWithId<T[]> repo, Guid userId, string id)
        => (await repo.GetAsync(userId, id)) ?? new EtaggedEntityWithId<T[]>(Array.Empty<T>(), String.Empty, id);
}