using ChargePlan.Service.Entities;
using ChargePlan.Service.Entities.ForecastTuning;

namespace ChargePlan.Service.Infrastructure;

public interface IForecastHistoryRepository : IEtaggedRepository<ForecastHistory>
{
}