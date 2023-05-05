using ChargePlan.Builder.Templates;

namespace ChargePlan.Service.Infrastructure;

public interface IUserPricingRepository : IRepository<IEnumerable<PriceAtAbsoluteTimes>>
{
}