public class UserProfileService
{
    private readonly IUserPlantRepository _plant;
    private readonly IUserDemandCompletedRepository _completed;

    public UserProfileService(IUserPlantRepository plant, IUserDemandCompletedRepository completed)
    {
        _plant = plant;
        _completed = completed;
    }

    public async Task<UserPlantParameters> GetPlantParameters(Guid userId)
    {
        return (await _plant.GetAsync(userId)) ?? new(new());
    }

    public Task<UserPlantParameters> PutPlantParameters(Guid userId, UserPlantParameters plant)
    {
        return _plant.UpsertAsync(userId, plant);
    }

    /// <summary>
    /// Record that a demand has been switched on, and that it no longer needs factoring into forthcoming calculations.
    /// Demand completions are identified by their unique hash of their name and datetime.
    /// </summary>
    public async Task<IEnumerable<DemandCompleted>> PostCompletedDemandAsHash(Guid userId, DemandCompleted demandCompleted)
    {
        var completedDemands = await _completed.GetAsyncOrEmpty(userId);

        if (completedDemands.Any(f => f.DemandHash == demandCompleted.DemandHash) == false)
        {
            completedDemands = completedDemands
                .Where(f => f.DateTime.AddMonths(1) < DateTime.Now) // Prune old ones
                .Append(demandCompleted);

            completedDemands = await _completed.UpsertAsync(userId, completedDemands);
        }

        return completedDemands;
    }
}