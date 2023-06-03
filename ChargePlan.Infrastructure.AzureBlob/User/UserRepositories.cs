using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public record UserRepositories(
    IUserPlantRepository Plant,
    IUserDemandRepository Demand,
    IUserShiftableDemandRepository Shiftable,
    IUserChargeRepository Charge,
    IUserPricingRepository Pricing,
    IUserExportRepository Export,
    IUserDayTemplatesRepository Days,
    IUserDemandCompletedRepository CompletedDemands,
    IUserRecommendationsRepository Recommendations
) : IUserRepositories;
