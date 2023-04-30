public class RecommendationService
{
    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IPlant _plant;

    public RecommendationService(IDirectNormalIrradianceProvider dniWeatherProvider, IPlant plant)
    {
        _plant = plant ?? throw new ArgumentNullException(nameof(plant));
        _dniWeatherProvider = dniWeatherProvider ?? throw new ArgumentNullException(nameof(dniWeatherProvider));
    }

    public async Task<Recommendations> CalculateRecommendations(Guid userId, ChargePlanExecutionParameters executionParameters)
    {
        var generation = await new WeatherBuilder(
                executionParameters.ArraySpecification.ArrayElevationDegrees,
                executionParameters.ArraySpecification.ArrayAzimuthDegrees,
                executionParameters.ArraySpecification.LatDegrees,
                executionParameters.ArraySpecification.LongDegrees)
            .WithArrayArea(executionParameters.ArraySpecification.ArrayArea)
            .WithDniSource(_dniWeatherProvider)
            .BuildAsync();

        var mainBuilder = new AlgorithmBuilder(_plant)
            .WithInitialBatteryEnergy(executionParameters.InitialBatteryEnergy)
            .WithGeneration(generation);

        foreach (var shiftable in executionParameters.ShiftableDemandAnyDay)
        {
            mainBuilder = mainBuilder.AddShiftableDemandAnyDay(shiftable.PowerAtRelativeTimes, shiftable.Priority);
        }

        var dayBuilder = mainBuilder.ForDay(DateTime.Today); // Doesn't matter, just a starting point.
        foreach (var days in executionParameters.Days)
        {
            dayBuilder = dayBuilder
                .ForEachDay(days.Dates.ToArray())
                .AddDemand(days.Demand)
                .AddChargeWindow(days.Charge)
                .AddPricing(days.Pricing)
                .AddExportPricing(days.Export);

            foreach (var shiftableDemand in days.ShiftableDemands)
            {
                dayBuilder = dayBuilder.AddShiftableDemand(shiftableDemand.PowerAtRelativeTimes, shiftableDemand.Priority);
            }
        }

        var algorithm = dayBuilder.Build();
        var recommendations = algorithm.DecideStrategy();
        return recommendations;
    }
}