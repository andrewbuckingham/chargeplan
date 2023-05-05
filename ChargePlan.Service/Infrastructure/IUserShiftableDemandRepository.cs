using ChargePlan.Builder.Templates;

namespace ChargePlan.Service.Infrastructure;

public interface IUserShiftableDemandRepository : IRepository<IEnumerable<PowerAtRelativeTimes>>
{
}