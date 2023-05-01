public interface IUserDayTemplatesRepository
{
    Task<ChargePlanTemplatedParameters?> GetAsync(Guid userId);
    Task<ChargePlanTemplatedParameters> UpsertAsync(Guid userId, ChargePlanTemplatedParameters entity);
}