using System.Text.Json;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;

namespace ChargePlan.Service;

public class UserProfileService
{
    private readonly ILogger _logger;
    private readonly UserPermissionsFacade _user;
    private readonly IUserPlantRepository _plant;
    private readonly IUserDemandCompletedRepository _completed;
    private readonly IUserShiftableDemandRepository _shiftable;

    public UserProfileService(ILogger<UserProfileService> logger, UserPermissionsFacade user, IUserPlantRepository plant, IUserDemandCompletedRepository completed, IUserShiftableDemandRepository shiftable)
    {
        _logger = logger;
        _user = user;
        _plant = plant;
        _completed = completed;
        _shiftable = shiftable;
    }

    public async Task<UserPlantParameters> GetPlantParameters()
    {
        return (await _plant.GetAsync(_user.Id)) ?? new(new());
    }

    public Task<UserPlantParameters> PutPlantParameters(UserPlantParameters plant)
    {
        return _plant.UpsertAsync(_user.Id, plant);
    }

    public async Task<IEnumerable<DemandCompleted>> PostCompletedDemandMatchFirstType(string shiftableDemandType)
    {
        // This is a temporary solution. Should really have an index of hashes from the last run.
        // Instead, synthesise all the hashes based on likely datetime ranges (today + {n..4} days)
        // Also calls the proper PostCompleted call many times. A bit hacky.
        var matchingDemands = (await _shiftable.GetAsyncOrEmpty(_user.Id))
            .Where(f => f.Type == shiftableDemandType)
            .CrossJoin(Enumerable.Range(1, 4)) // Where 4 is the max number of days most demands are likely to span...
            .Select(f => f.Item1.AsShiftableDemand(
                Domain.ShiftableDemandPriority.Low,
                (DateTime.Today, DateTime.Today.AddDays(f.Item2)),
                null));
        
        foreach (var demand in matchingDemands)
        {
            await PostCompletedDemandAsHash(new DemandCompleted(
                demand.AsDemandHash(),
                DateTime.Now,
                demand.Name
            ));
        }

        return (await _completed.GetAsyncOrEmpty(_user.Id)).Entity.Where(f => f.DateTime.Date == DateTime.Today);
    }

    /// <summary>
    /// Record that a demand has been switched on, and that it no longer needs factoring into forthcoming calculations.
    /// Demand completions are identified by their unique hash of their name and datetime.
    /// </summary>
    public async Task<IEnumerable<DemandCompleted>> PostCompletedDemandAsHash(DemandCompleted demandCompleted)
        => await Policy
            .Handle<ConcurrencyException>()
            .WaitAndRetryAsync(4, f => TimeSpan.FromSeconds(f))
            .ExecuteAsync(async () =>
        {
            EtaggedEntity<DemandCompleted[]> completedDemands;

            try
            {
                completedDemands = await _completed.GetAsyncOrEmpty(_user.Id);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Demand completions JSON is invalid. Replacing with empty structure.");
                completedDemands = new(new DemandCompleted[] { }, String.Empty);
            }

            if (completedDemands.Entity.Any(f => f.DemandHash == demandCompleted.DemandHash) == false)
            {
                completedDemands = completedDemands with
                {
                    Entity = completedDemands.Entity
                        .Where(f => f.DateTime > DateTime.Now.AddMonths(-1)) // Prune old ones
                        .Append(demandCompleted)
                        .ToArray()
                };

                completedDemands = await _completed.UpsertAsync(_user.Id, completedDemands);
            }

            return completedDemands.Entity;
        });
}