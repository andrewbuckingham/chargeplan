namespace ChargePlan.Domain;

public interface IDemandProfile : ISplineable<DemandValue>
{
    DateTime Starting { get; }
    DateTime Until { get; }
    string Name { get; }
    string Type { get; }
}

public record DemandValue(DateTime DateTime, float Power);