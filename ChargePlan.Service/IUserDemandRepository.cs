public interface IUserDemandRepository
{
    Task<IEnumerable<PowerAtAbsoluteTimes>?> GetAsync(Guid userId);
    Task<IEnumerable<PowerAtAbsoluteTimes>> UpsertAsync(Guid userId, IEnumerable<PowerAtAbsoluteTimes> entities);
}