using ChargePlan.Builder.Templates;

namespace ChargePlan.Service.Infrastructure;

public interface IUserDemandRepository : IRepository<IEnumerable<PowerAtAbsoluteTimes>>
{
}