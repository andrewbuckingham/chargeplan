using ChargePlan.Builder.Templates;

namespace ChargePlan.Service.Infrastructure;

public interface IUserExportRepository : IRepository<IEnumerable<PriceAtAbsoluteTimes>>
{
}