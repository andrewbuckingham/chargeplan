using ChargePlan.Domain.Splines;
using ChargePlan.Weather;
using ChargePlan.Service;
using ChargePlan.Service.Infrastructure;
using Moq;
using Microsoft.Extensions.Logging;
using ChargePlan.Service.Entities.ForecastTuning;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

namespace ChargePlan.Service.UnitTests;

public class ForecastTuning
{

    private static Mock<IForecastHistoryRepository> ForecastHistoryRepo(params ForecastDatapoint[] datapoints)
    {
        ForecastHistory fh = new();
        fh.Values = datapoints.ToList();
        EtaggedEntity<ForecastHistory>? entity = new(fh, Guid.Empty.ToString());

        var forecastRepo = new Mock<IForecastHistoryRepository>();
        forecastRepo.Setup(f=>f.GetAsync(It.IsAny<Guid>())).Returns(Task.FromResult(entity));
        return forecastRepo;
    }
    private static Mock<IEnergyHistoryRepository> EnergyHistoryRepo(params EnergyDatapoint[] datapoints)
    {
        EnergyHistory fh = new();
        fh.Values = datapoints.ToList();
        EtaggedEntity<EnergyHistory>? entity = new(fh, Guid.Empty.ToString());

        var energyRepo = new Mock<IEnergyHistoryRepository>();
        energyRepo.Setup(f=>f.GetAsync(It.IsAny<Guid>())).Returns(Task.FromResult(entity));
        return energyRepo;
    }

    private static Mock<IForecastHistoryRepository> ForecastHistoryRepoHelper(int totalPoints, int numberOfForecasts, float energy)
    {
        var singleForecast = Enumerable.Range(1,totalPoints).Select(i => new ForecastDatapoint(
            ForHour: DateTime.Today.AddHours(i),
            Energy: energy,
            ProducedAt: DateTime.Today,
            CloudCoverPercent: 0
        )).ToArray();

        var forecastsBackInTime = Enumerable.Range(0, numberOfForecasts)
            .SelectMany(i => singleForecast.Select(f=> f with { ProducedAt = f.ProducedAt.AddHours(-i)}))
            .ToArray();

        return ForecastHistoryRepo(forecastsBackInTime);
    }

    private static Mock<IEnergyHistoryRepository> EnergyHistoryRepoHelper(int totalPoints, float energy)
       => EnergyHistoryRepo(Enumerable.Range(1,totalPoints).Select(i => new EnergyDatapoint(
            InHour: DateTime.Today.AddHours(i),
            Energy: energy
       )).ToArray());
    

    [Fact]
    public async Task TotallyAccurateForecast_HasScalarOfUnity()
    {
        var logger = new Mock<ILogger<ForecastTuningService>>();
        var userAuthRepo = new Mock<IUserAuthorisationRepository>();
        var userId = new Mock<IUserIdAccessor>();

        var forecastRepo = ForecastHistoryRepoHelper(16, 24, 1.0f);
        var energyRepo = EnergyHistoryRepoHelper(16, 1.0f);

        userId.Setup(f=>f.UserId).Returns(Guid.NewGuid());
        
        var svc = new ForecastTuningService(
            logger.Object,
            new Facades.UserPermissionsFacade(userId.Object, userAuthRepo.Object),
            null,
            forecastRepo.Object,
            energyRepo.Object,
            null,
            null
            );


        var result = await svc.DetermineLatestForecastScalar();

        Assert.Equal(1.0f, result.SunlightScalar);
    }

    [Fact]
    public async Task ForecastHalfTheActual_HasScalarOfDouble()
    {
        var logger = new Mock<ILogger<ForecastTuningService>>();
        var userAuthRepo = new Mock<IUserAuthorisationRepository>();
        var userId = new Mock<IUserIdAccessor>();

        var forecastRepo = ForecastHistoryRepoHelper(16, 24, 0.5f);
        var energyRepo = EnergyHistoryRepoHelper(16, 1.0f);

        userId.Setup(f=>f.UserId).Returns(Guid.NewGuid());
        
        var svc = new ForecastTuningService(
            logger.Object,
            new Facades.UserPermissionsFacade(userId.Object, userAuthRepo.Object),
            null,
            forecastRepo.Object,
            energyRepo.Object,
            null,
            null
            );


        var result = await svc.DetermineLatestForecastScalar();

        Assert.Equal(2.0f, result.SunlightScalar);
    }
}
