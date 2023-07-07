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
    private readonly IUserRecommendationsRepository _recommendations;

    public UserProfileService(ILogger<UserProfileService> logger, UserPermissionsFacade user, IUserPlantRepository plant, IUserDemandCompletedRepository completed, IUserRecommendationsRepository recommendations)
    {
        _logger = logger;
        _user = user;
        _plant = plant;
        _completed = completed;
        _recommendations = recommendations;
    }

    public async Task<UserPlantParameters> GetPlantParameters()
    {
        return (await _plant.GetAsync(_user.Id)) ?? new();
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
                        .Where(f => f.DateTime > DateTime.Now.AddMonths(-1).ToLocalTime()) // Prune old ones
                        .Append(demandCompleted)
                        .ToArray()
                };

                completedDemands = await _completed.UpsertAsync(_user.Id, completedDemands);
            }

            return completedDemands.Entity;
        });

    public async Task<IEnumerable<DemandCompleted>> GetCompletedDemandsToday()
        => await Policy
            .Handle<ConcurrencyException>()
            .WaitAndRetryAsync(4, f => TimeSpan.FromSeconds(f))
            .ExecuteAsync(async () =>
        {
            var completedDemands = await _completed.GetAsyncOrEmpty(_user.Id);
            var result = completedDemands.Entity.Where(f => f.DateTime.Date == DateTime.Today).ToArray();

            return result;
        });

    public async Task<IEnumerable<DemandCompleted>> GetCompletedDemandsTodayType(string type)
        => await Policy
            .Handle<ConcurrencyException>()
            .WaitAndRetryAsync(4, f => TimeSpan.FromSeconds(f))
            .ExecuteAsync(async () =>
        {
            var completedDemands = await _completed.GetAsyncOrEmpty(_user.Id);
            var result = completedDemands.Entity.Where(f => f.DateTime.Date == DateTime.Today && f.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            return result;
        });

    public async Task<IEnumerable<DemandCompleted>> PostCompletedDemandTodayType(string shiftableDemandType)
        => await Policy
                .Handle<ConcurrencyException>()
                .WaitAndRetryAsync(4, f => TimeSpan.FromSeconds(f))
                .ExecuteAsync(async () =>
        {
            var matchingDemand = (await _recommendations.GetAsync(_user.Id) ?? throw new InvalidStateException("There is no stored data from the last run. Please run a demand calculation."))
                .ShiftableDemands
                .Where(f => f.StartAt.Date == DateTime.Today && f.Type.Equals(shiftableDemandType, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault() ?? throw new NotFoundException();
                
            await PostCompletedDemandAsHash(new DemandCompleted(
                matchingDemand.DemandHash,
                DateTime.Now.ToLocalTime(),
                matchingDemand.Name,
                matchingDemand.Type
            ));

            return (await _completed.GetAsyncOrEmpty(_user.Id)).Entity
                .Where(f => f.DateTime.Date == DateTime.Today && f.Type.Equals(shiftableDemandType, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
        });

    public async Task DeleteCompletedDemandTodayType(string type)
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

            if (completedDemands.Entity.Any(f => f.DateTime.Date == DateTime.Today && f.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase)))
            {
                completedDemands = completedDemands with
                {
                    Entity = completedDemands.Entity
                        .Where(f => f.DateTime > DateTime.Now.AddMonths(-1).ToLocalTime()) // Prune old ones
                        .Where(f => !(f.DateTime.Date == DateTime.Today && f.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase)))
                        .ToArray()
                };

                completedDemands = await _completed.UpsertAsync(_user.Id, completedDemands);
            }
            else
            {
                throw new NotFoundException();
            }
        });
}
