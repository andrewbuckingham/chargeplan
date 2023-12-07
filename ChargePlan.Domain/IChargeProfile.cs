namespace ChargePlan.Domain;

public interface IChargeProfile : ISplineable<ChargeValue>
{
    IEnumerable<ChargeValue> Values { get; }
}

public record ChargeValue(DateTime DateTime, float Power);