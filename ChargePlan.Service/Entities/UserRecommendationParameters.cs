namespace ChargePlan.Service.Entities;

public record UserRecommendationParameters(
    float InitialBatteryEnergy,
    int DaysToRecommendOver = 3
);