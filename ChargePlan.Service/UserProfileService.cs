public class UserProfileService
{
    private readonly IUserPlantRepository _plant;

    public UserProfileService(IUserPlantRepository plant)
    {
        _plant = plant;
    }

    public async Task<UserPlantParameters> GetPlantParameters(Guid userId)
    {
        return (await _plant.GetAsync(userId)) ?? new(new());
    }

    public Task<UserPlantParameters> PutPlantParameters(Guid userId, UserPlantParameters plant)
    {
        return _plant.UpsertAsync(userId, plant);
    }
}