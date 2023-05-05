namespace ChargePlan.Service.Entities;

public record UserPlantParameters(
    ArraySpecification ArraySpecification,
    string PlantType = "Hy36"
);

public record ArraySpecification(
    float ArrayArea = 0.0f,
    float ArrayElevationDegrees = 45.0f, float ArrayAzimuthDegrees = 0.0f,
    float LatDegrees = 54.5f, float LongDegrees = -1.55f
);