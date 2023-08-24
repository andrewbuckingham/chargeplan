using ChargePlan.Service.Entities;

namespace ChargePlan.Service.Infrastructure;

public interface IForecastHistoryRepository : IEtaggedRepository<ForecastHistory>
{
}