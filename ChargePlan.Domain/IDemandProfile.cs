namespace ChargePlan.Domain;

public interface IDemandProfile : ISplineable<DemandValue>
{
    DateTime Starting { get; }
    DateTime Until { get; }
}

public record DemandValue(DateTime DateTime, float Power);