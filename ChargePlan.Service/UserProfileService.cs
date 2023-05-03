public class UserProfileService
{
    private readonly UserPermissionsFacade _user;
    private readonly IUserPlantRepository _plant;
    private readonly IUserDemandCompletedRepository _completed;

    public UserProfileService(UserPermissionsFacade user, IUserPlantRepository plant, IUserDemandCompletedRepository completed)
    {
        _user = user;
        _plant = plant;
        _completed = completed;
    }

    public async Task<UserPlantParameters> GetPlantParameters()
    {
        return (await _plant.GetAsync(_user.Id)) ?? new(new());
    }

    public Task<UserPlantParameters> PutPlantParameters(UserPlantParameters plant)
    {
        return _plant.UpsertAsync(_user.Id, plant);
    }

    /// <summary>
    /// Record that a demand has been switched on, and that it no longer needs factoring into forthcoming calculations.
    /// Demand completions are identified by their unique hash of their name and datetime.
    /// </summary>
    public async Task<IEnumerable<DemandCompleted>> PostCompletedDemandAsHash(DemandCompleted demandCompleted)
    {
        var completedDemands = await _completed.GetAsyncOrEmpty(_user.Id);

        if (completedDemands.Any(f => f.DemandHash == demandCompleted.DemandHash) == false)
        {
            completedDemands = completedDemands
                .Where(f => f.DateTime.AddMonths(1) < DateTime.Now) // Prune old ones
                .Append(demandCompleted);

            completedDemands = await _completed.UpsertAsync(_user.Id, completedDemands);
        }

        return completedDemands;
    }
}