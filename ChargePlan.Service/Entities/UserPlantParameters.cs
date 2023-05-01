public record UserPlantParameters(
    ArraySpecification ArraySpecification,
    string PlantType = "Hy36"
)
{
    IPlant GetPlant => new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f);
}