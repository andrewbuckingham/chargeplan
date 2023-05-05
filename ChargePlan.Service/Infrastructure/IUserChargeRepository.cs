using ChargePlan.Builder.Templates;

namespace ChargePlan.Service.Infrastructure;

public interface IUserChargeRepository : IRepository<IEnumerable<PowerAtAbsoluteTimes>>
{
}