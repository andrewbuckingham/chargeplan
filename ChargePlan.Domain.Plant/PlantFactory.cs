using ChargePlan.Domain.Exceptions;

namespace ChargePlan.Domain.Plant;

public class PlantFactory : IPlantFactory
{
    public IPlant CreatePlant(string plantType) => plantType switch
    {
        "Hy36" => new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f),
        _ => throw new InvalidStateException($"{plantType} is not a recognised plant type")
    };
}