using ChargePlan.Domain.Solver;

namespace ChargePlan.Service.Infrastructure;

public interface IUserDemandCompletedRepository : IEtaggedRepository<DemandCompleted[]>
{
}