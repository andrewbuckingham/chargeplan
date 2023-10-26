using ChargePlan.Domain;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Entities.ForecastTuning;

namespace ChargePlan.Service.Infrastructure;

public interface IEnergyHistoryRepository : IEtaggedRepositoryByMonth<EnergyHistory>
{
}