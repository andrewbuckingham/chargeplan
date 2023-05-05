namespace ChargePlan.Domain;

public interface IChargeProfile : ISplineable<ChargeValue>
{
}

public record ChargeValue(DateTime DateTime, float Power);