public record UserPlantParameters(
    ArraySpecification ArraySpecification,
    string PlantType = "Hy36"
)
{
    public IPlant GetPlant() => new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f); // Stub until implemented other plant types
};

public record ArraySpecification(
    float ArrayArea = 0.0f,
    float ArrayElevationDegrees = 45.0f, float ArrayAzimuthDegrees = 0.0f,
    float LatDegrees = 54.5f, float LongDegrees = -1.55f
);