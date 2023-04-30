public interface IUserShiftableDemandRepository
{
    Task<IEnumerable<PowerAtRelativeTimes>?> GetAsync(Guid userId);
    Task<IEnumerable<PowerAtRelativeTimes>> UpsertAsync(Guid userId, IEnumerable<PowerAtRelativeTimes> entities);
}