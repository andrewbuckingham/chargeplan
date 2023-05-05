using ChargePlan.Builder;
using ChargePlan.Domain;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Weather;

namespace ChargePlan.Service;

public class AdhocRecommendationService
{
    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IPlantFactory _plantFactory;

    public AdhocRecommendationService(IDirectNormalIrradianceProvider dniWeatherProvider, IPlantFactory plantFactory)
    {
        _plantFactory = plantFactory ?? throw new ArgumentNullException(nameof(plantFactory));
        _dniWeatherProvider = dniWeatherProvider ?? throw new ArgumentNullException(nameof(dniWeatherProvider));
    }

    public async Task<Recommendations> CalculateRecommendations(ChargePlanAdhocParameters input)
    {
        var generation = await new WeatherBuilder(
                input.Plant.ArraySpecification.ArrayElevationDegrees,
                input.Plant.ArraySpecification.ArrayAzimuthDegrees,
                input.Plant.ArraySpecification.LatDegrees,
                input.Plant.ArraySpecification.LongDegrees)
            .WithArrayArea(input.Plant.ArraySpecification.ArrayArea)
            .WithDniSource(_dniWeatherProvider)
            .BuildAsync();

        IPlant plant = _plantFactory.CreatePlant(input.Plant.PlantType);

        var mainBuilder = new AlgorithmBuilder(plant)
            .WithInitialBatteryEnergy(input.InitialBatteryEnergy)
            .WithGeneration(generation);

        foreach (var shiftable in input.ShiftableDemandsAnyDay)
        {
            mainBuilder = mainBuilder.AddShiftableDemandAnyDay(shiftable.PowerAtRelativeTimes, priority: shiftable.Priority);
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
                dayBuilder = dayBuilder.AddShiftableDemand(shiftableDemand.PowerAtRelativeTimes, shiftableDemand.Priority, null);
            }
        }

        var algorithm = dayBuilder.Build();
        var recommendations = algorithm.DecideStrategy();
        return recommendations;
    }
}