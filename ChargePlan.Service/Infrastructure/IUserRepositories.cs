namespace ChargePlan.Service.Infrastructure;

public interface IUserRepositories
{
    IUserPlantRepository Plant { get; }
    IUserDemandRepository Demand { get; }
    IUserShiftableDemandRepository Shiftable { get; }
    IUserChargeRepository Charge { get; }
    IUserPricingRepository Pricing { get; }
    IUserExportRepository Export { get; }
    IUserDayTemplatesRepository Days { get; }
    IUserDemandCompletedRepository CompletedDemands { get; }
    IUserRecommendationsRepository Recommendations { get; }
}
