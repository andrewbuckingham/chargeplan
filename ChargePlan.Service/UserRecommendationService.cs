public class UserRecommendationService
{
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

    public UserRecommendationService(
        IDirectNormalIrradianceProvider dniWeatherProvider,
        IPlantFactory plantFactory,
        IUserPlantRepository plant,
        IUserDemandRepository demand,
        IUserShiftableDemandRepository shiftable,
        IUserChargeRepository charge,
        IUserPricingRepository pricing,
        IUserExportRepository export,
        IUserDayTemplatesRepository days,
        IUserDemandCompletedRepository completedDemands)
    {
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
    }
    public async Task<Recommendations> CalculateRecommendations(Guid userId, UserRecommendationParameters parameters)
    {
        var plantSpec = await _plant.GetAsync(userId) ?? new(new());
        var input = await _days.GetAsync(userId) ?? throw new InvalidOperationException("Must defined day templates first");
        var allShiftable = await _shiftable.GetAsyncOrEmpty(userId);
        var allDemands = await _demand.GetAsyncOrEmpty(userId);
        var allCharge = await _charge.GetAsyncOrEmpty(userId);
        var allPricing = await _pricing.GetAsyncOrEmpty(userId);
        var allExport = await _export.GetAsyncOrEmpty(userId);
        var completedDemands = await _completedDemands.GetAsyncOrEmpty(userId);

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
            .ExcludingCompletedDemands(completedDemands);

        var dayBuilder = mainBuilder.ForDay(DateTime.Today); // Doesn't matter, just a starting point.

        foreach (var shiftable in input.ShiftableDemandsAnyDay.Where(f => f.Disabled == false))
        {
            dayBuilder = dayBuilder.ForEachDay(shiftable.ApplicableDatesStartingFrom(DateTime.Today).ToArray());

            var demand = allShiftable.OnlyOne(f => f.Name, shiftable.Name);
            dayBuilder = dayBuilder.AddShiftableDemand(demand, shiftable.Priority);
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
                dayBuilder = dayBuilder.AddShiftableDemand(shiftable, shiftableDemand.Priority);
            }
        }

        var algorithm = dayBuilder.Build();
        var recommendations = algorithm.DecideStrategy();
        return recommendations;
    }
}