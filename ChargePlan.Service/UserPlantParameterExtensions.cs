using ChargePlan.Service.Entities;
using ChargePlan.Weather;

namespace ChargePlan.Service;

public static class PlantExtensions
{
    public static WeatherBuilder AsWeatherBuilder(this UserPlantParameters plantSpec)
        => new WeatherBuilder(
                plantSpec.ArraySpecification.ArrayElevationDegrees,
                plantSpec.ArraySpecification.ArrayAzimuthDegrees,
                plantSpec.ArraySpecification.LatDegrees,
                plantSpec.ArraySpecification.LongDegrees)
            .WithArrayArea(plantSpec.ArraySpecification.ArrayArea, absolutePeakWatts: plantSpec.ArraySpecification.AbsolutePeakWatts)
            .AddShading(plantSpec.ArrayShading);
}