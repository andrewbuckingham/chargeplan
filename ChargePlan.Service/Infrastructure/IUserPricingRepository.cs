public interface IUserPricingRepository
{
    Task<IEnumerable<PriceAtAbsoluteTimes>?> GetAsync(Guid userId);
    Task<IEnumerable<PriceAtAbsoluteTimes>> UpsertAsync(Guid userId, IEnumerable<PriceAtAbsoluteTimes> entities);
}