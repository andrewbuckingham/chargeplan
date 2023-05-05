namespace ChargePlan.Domain;

public interface IPlantFactory
{
    IPlant CreatePlant(string plantType);
}