using ChargePlan.Builder;
using ChargePlan.Domain;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;
using ChargePlan.Weather;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Service;

public class UserRecommendationService
{
    private readonly UserPermissionsFacade _user;
    private readonly ILogger _logger;

    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IPlantFactory _plantFactory;

    private readonly IUserRepositories _repos;

    public UserRecommendationService(
        UserPermissionsFacade user,
        ILogger<UserRecommendationService> logger,
        IDirectNormalIrradianceProvider dniWeatherProvider,
        IPlantFactory plantFactory,
        IUserRepositories repos)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _dniWeatherProvider = dniWeatherProvider ?? throw new ArgumentNullException(nameof(dniWeatherProvider));
        _plantFactory = plantFactory ?? throw new ArgumentNullException(nameof(plantFactory));

        _repos = repos ?? throw new ArgumentNullException(nameof(repos));
    }

    public async Task<Recommendations> CalculateRecommendations(UserRecommendationParameters parameters)
    {
        var plantSpec = await _repos.Plant.GetAsync(_user.Id) ?? new(new());
        var input = await _repos.Days.GetAsync(_user.Id) ?? throw new InvalidStateException("Must defined day templates first");
        var allShiftable = await _repos.Shiftable.GetAsyncOrEmpty(_user.Id);
        var allDemands = await _repos.Demand.GetAsyncOrEmpty(_user.Id);
        var allCharge = await _repos.Charge.GetAsyncOrEmpty(_user.Id);
        var allPricing = await _repos.Pricing.GetAsyncOrEmpty(_user.Id);
        var allExport = await _repos.Export.GetAsyncOrEmpty(_user.Id);
        var completedDemands = await _repos.CompletedDemands.GetAsyncOrEmpty(_user.Id);

        IPlant plant = _plantFactory.CreatePlant(plantSpec.PlantType);

        var generation = await new WeatherBuilder(
                plantSpec.ArraySpecification.ArrayElevationDegrees,
                plantSpec.ArraySpecification.ArrayAzimuthDegrees,
                plantSpec.ArraySpecification.LatDegrees,
                plantSpec.ArraySpecification.LongDegrees)
            .WithArrayArea(plantSpec.ArraySpecification.ArrayArea)
            .WithDniSource(_dniWeatherProvider)
            .BuildAsync();

        var mainBuilder = new AlgorithmBuilder(plant)
            .WithInitialBatteryEnergy(parameters.InitialBatteryEnergy)
            .WithGeneration(generation)
            .ExcludingCompletedDemands(completedDemands.Entity);

        foreach (var shiftable in input.ShiftableDemandsAnyDay.Where(f => f.Disabled == false))
        {
            var demand = allShiftable.OnlyOne(f => f.Name, shiftable.Name);
            mainBuilder = mainBuilder.AddShiftableDemandAnyDay(demand,
                noSoonerThan: DateTime.Today.AddYears(-1),
                noLaterThan: DateTime.Today.AddDays(shiftable.OverNumberOfDays),
                shiftable.Priority,
                shiftable.DontRepeatWithin);
        }

        var days = input.DayTemplates
            .Select(f =>
            {
                int dayNumber = (int)DateTime.Today.DayOfWeek - (int)f.DayOfWeek;
                if (dayNumber < 0) dayNumber += 7;

                DateTime date = DateTime.Today.AddDays(dayNumber);
                return (Date: date, Template: f);
            })
            .OrderBy(f => f.Date)
            .Take(parameters.DaysToRecommendOver);

        var dayBuilder = mainBuilder.ForDay(DateTime.Today); // Doesn't matter, just a starting point.
        foreach (var day in days)
        {
            var demand = allDemands.OnlyOne(f => f.Name, day.Template.DemandName);
            var charge = allCharge.OnlyOne(f => f.Name, day.Template.ChargeName);
            var pricing = allPricing.OnlyOne(f => f.Name, day.Template.PricingName);
            var export = allExport.OnlyOne(f => f.Name, day.Template.ExportName);

            dayBuilder = dayBuilder
                .ForDay(day.Date)
                .AddDemand(demand)
                .AddChargeWindow(charge)
                .AddPricing(pricing)
                .AddExportPricing(export);

            foreach (var shiftableDemand in day.Template.ShiftableDemands.Where(f => f.Disabled == false))
            {
                var shiftable = allShiftable.OnlyOne(f => f.Name, shiftableDemand.Name);
                dayBuilder = dayBuilder.AddShiftableDemand(shiftable, shiftableDemand.Priority, shiftableDemand.DontRepeatWithin);
            }
        }

        var algorithm = dayBuilder.Build();
        var recommendations = algorithm.DecideStrategy();

        try
        {
            await _repos.Recommendations.UpsertAsync(_user.Id, recommendations);
        }
        catch (InfrastructureException iex)
        {
            _logger.LogError($"Couldn't write latest recommendation for {_user.Id}", iex);
        }

        return recommendations;
    }

    public Task<Recommendations?> GetLastRecommendation()
        => _repos.Recommendations.GetAsync(_user.Id);
}