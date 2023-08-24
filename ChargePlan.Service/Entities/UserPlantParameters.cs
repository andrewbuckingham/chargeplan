using ChargePlan.Weather;

namespace ChargePlan.Service.Entities;

public record UserPlantParameters(
    ArraySpecification ArraySpecification,
    WeatherForecastSettings WeatherForecastSettings,
    AlgorithmSettings AlgorithmSettings,
    Shading[] ArrayShading,
    string PlantType = "Hy36"
)
{
    public UserPlantParameters() : this(
        new(),
        new(),
        new(),
        new Shading[] {
            // new Shading(
            //     (0, 0),
            //     (35, 85),
            //     (5, 100),
            //     (15, 110),
            //     (5, 120),
            //     (20, 180),
            //     (15, 195),
            //     (5, 195),
            //     (5, 205),
            //     (15, 220),
            //     (10, 250),
            //     (5, 255),
            //     (5, 265),
            //     (25, 270),
            //     (25, 300),
            //     (0, 300)
            //     ).WithAzimuthRotatedBy(-180)
        }, "Hy36") { }
}

public record ArraySpecification(
    float ArrayArea = 0.0f, int? AbsolutePeakWatts = null,
    float ArrayElevationDegrees = 45.0f, float ArrayAzimuthDegrees = 0.0f,
    float LatDegrees = 54.5f, float LongDegrees = -1.55f
);

public record WeatherForecastSettings(
    float SunlightScalar = 1.0f,
    float OvercastScalar = 1.0f
);

public record AlgorithmSettings(
    float ChargeRateLimitScalar = 1.0f
);