namespace ChargePlan.Domain;

public interface IChargeProfile : ISplineable<ChargeValue>
{
    List<ChargeValue> Values { get; }
}

public record ChargeValue(DateTime DateTime, float Power);