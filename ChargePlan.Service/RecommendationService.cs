public class RecommendationService
{
    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IPlant _plant;

    public RecommendationService(IDirectNormalIrradianceProvider dniWeatherProvider, IPlant plant)
    {
        _plant = plant ?? throw new ArgumentNullException(nameof(plant));
        _dniWeatherProvider = dniWeatherProvider ?? throw new ArgumentNullException(nameof(dniWeatherProvider));
    }

    public async Task<Recommendations> CalculateRecommendations(Guid userId, ChargePlanAdhocParameters input)
    {
        var generation = await new WeatherBuilder(
                input.Plant.ArraySpecification.ArrayElevationDegrees,
                input.Plant.ArraySpecification.ArrayAzimuthDegrees,
                input.Plant.ArraySpecification.LatDegrees,
                input.Plant.ArraySpecification.LongDegrees)
            .WithArrayArea(input.Plant.ArraySpecification.ArrayArea)
            .WithDniSource(_dniWeatherProvider)
            .BuildAsync();

        var mainBuilder = new AlgorithmBuilder(_plant)
            .WithInitialBatteryEnergy(input.InitialBatteryEnergy)
            .WithGeneration(generation);

        foreach (var shiftable in input.ShiftableDemandsAnyDay)
        {
            mainBuilder = mainBuilder.AddShiftableDemandAnyDay(shiftable.PowerAtRelativeTimes, shiftable.Priority);
        }

        var dayBuilder = mainBuilder.ForDay(DateTime.Today); // Doesn't matter, just a starting point.
        foreach (var days in input.Days)
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