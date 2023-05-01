public interface IUserPlantRepository
{
    Task<UserPlantParameters?> GetAsync(Guid userId);
    Task<UserPlantParameters> UpsertAsync(Guid userId, UserPlantParameters entity);
}