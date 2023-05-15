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

    private readonly IUserPlantRepository _plant;
    private readonly IUserDemandRepository _demand;
    private readonly IUserShiftableDemandRepository _shiftable;
    private readonly IUserChargeRepository _charge;
    private readonly IUserPricingRepository _pricing;
    private readonly IUserExportRepository _export;
    private readonly IUserDayTemplatesRepository _days;
    private readonly IUserDemandCompletedRepository _completedDemands;
    private readonly IUserRecommendationsRepository _recommendations;

    public UserRecommendationService(
        UserPermissionsFacade user,
        ILogger<UserRecommendationService> logger,
        IDirectNormalIrradianceProvider dniWeatherProvider,
        IPlantFactory plantFactory,
        IUserPlantRepository plant,
        IUserDemandRepository demand,
        IUserShiftableDemandRepository shiftable,
        IUserChargeRepository charge,
        IUserPricingRepository pricing,
        IUserExportRepository export,
        IUserDayTemplatesRepository days,
        IUserDemandCompletedRepository completedDemands,
        IUserRecommendationsRepository recommendations)
    {
        _user = user;
        _logger = logger;

        _dniWeatherProvider = dniWeatherProvider ?? throw new ArgumentNullException(nameof(dniWeatherProvider));
        _plantFactory = plantFactory ?? throw new ArgumentNullException(nameof(plantFactory));

        _plant = plant;
        _demand = demand;
        _shiftable = shiftable;
        _charge = charge;
        _pricing = pricing;
        _export = export;
        _days = days;

        _completedDemands = completedDemands;
        _recommendations = recommendations;
    }

    public async Task<Recommendations> CalculateRecommendations(UserRecommendationParameters parameters)
    {
        var plantSpec = await _plant.GetAsync(_user.Id) ?? new(new());
        var input = await _days.GetAsync(_user.Id) ?? throw new InvalidStateException("Must defined day templates first");
        var allShiftable = await _shiftable.GetAsyncOrEmpty(_user.Id);
        var allDemands = await _demand.GetAsyncOrEmpty(_user.Id);
        var allCharge = await _charge.GetAsyncOrEmpty(_user.Id);
        var allPricing = await _pricing.GetAsyncOrEmpty(_user.Id);
        var allExport = await _export.GetAsyncOrEmpty(_user.Id);
        var completedDemands = await _completedDemands.GetAsyncOrEmpty(_user.Id);

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
            await _recommendations.UpsertAsync(_user.Id, recommendations);
        }
        catch (InfrastructureException iex)
        {
            _logger.LogError($"Couldn't write latest recommendation for {_user.Id}", iex);
        }

        return recommendations;
    }

    public Task<Recommendations?> GetLastRecommendation()
        => _recommendations.GetAsync(_user.Id);
}