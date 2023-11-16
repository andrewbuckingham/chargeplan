using ChargePlan.Domain.Splines;
using ChargePlan.Weather;
using ChargePlan.Service;
using ChargePlan.Service.Infrastructure;
using Moq;
using Microsoft.Extensions.Logging;
using ChargePlan.Service.Entities.ForecastTuning;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using System.ComponentModel;

namespace ChargePlan.Service.UnitTests;

public class ForecastTuning
{
    private static Mock<IForecastHistoryRepository> ForecastHistoryRepo(params ForecastDatapoint[] datapoints)
    {
        ForecastHistory fh = new();
        fh.Values = datapoints.ToList();
        EtaggedEntityWithId<ForecastHistory>? entity = new(fh, Guid.Empty.ToString(), "123");

        var forecastRepo = new Mock<IForecastHistoryRepository>();
        forecastRepo
            .Setup(f => f.GetAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(entity);

        return forecastRepo;
    }
    private static Mock<IEnergyHistoryRepository> EnergyHistoryRepo(params EnergyDatapoint[] datapoints)
    {
        EnergyHistory fh = new();
        fh.Values = datapoints.ToList();
        EtaggedEntityWithId<EnergyHistory>? entity = new(fh, Guid.Empty.ToString(), "123");

        var energyRepo = new Mock<IEnergyHistoryRepository>();
        energyRepo.Setup(f => f.GetAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(entity));
        return energyRepo;
    }

    private static Mock<IForecastHistoryRepository> ForecastHistoryRepoHelper(int totalPoints, int numberOfForecasts, float energy)
    {
        var singleForecast = Enumerable.Range(1,totalPoints).Select(i => new ForecastDatapoint(
            ForHour: DateTime.Today.AddHours(-i),
            Energy: energy,
            ProducedAt: DateTime.Today.AddHours(-i-1),
            CloudCoverPercent: 0
        )).ToArray();

        var forecastsBackInTime = Enumerable.Range(0, numberOfForecasts)
            .SelectMany(i => singleForecast.Select(f=> f with { ProducedAt = f.ProducedAt.AddHours(-i)}))
            .ToArray();

        return ForecastHistoryRepo(forecastsBackInTime);
    }

    private static Mock<IEnergyHistoryRepository> EnergyHistoryRepoHelper(int totalPoints, float energy)
       => EnergyHistoryRepo(Enumerable.Range(1,totalPoints).Select(i => new EnergyDatapoint(
            InHour: DateTime.Today.AddHours(-i),
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

        userId.Setup(f => f.UserId).Returns(Guid.NewGuid());
        
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

        userId.Setup(f => f.UserId).Returns(Guid.NewGuid());
        
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

    [Fact]
    public async Task ForecastUsesCorrectTimeHorizon_IgnoringOtherForecastLengths()
    {
        var logger = new Mock<ILogger<ForecastTuningService>>();
        var userAuthRepo = new Mock<IUserAuthorisationRepository>();
        var userId = new Mock<IUserIdAccessor>();

        float energyAtRelevantTimes = 2.0f;
        float energyAtOtherTimes = 1.0f;
        ForecastTuningSettings settings = new()
        {
            ForecastLengthToOptimiseFor = TimeSpan.FromHours(2),
            PeriodToAverageOver = TimeSpan.FromDays(4)
        };

        var singleForecast = Enumerable.Range(0, 24 * 7).Select(i => new ForecastDatapoint(
            ForHour: DateTime.Today.AddHours(-i),
            Energy: energyAtOtherTimes,
            ProducedAt: DateTime.Today.AddHours(-i),
            CloudCoverPercent: 0
        )).ToArray();

        var forecastsBackInTime = Enumerable.Range(0, 24)
            .SelectMany(i => singleForecast.Select(f => f with { ProducedAt = f.ProducedAt.AddHours(-i)}))
            .ToArray();

        // Just pick out the relevant times so those are used for the forecast.
        forecastsBackInTime = forecastsBackInTime.Select(f=>
            f.ForecastLength == settings.ForecastLengthToOptimiseFor
            ? f with { Energy = energyAtRelevantTimes }
            : f)
            .OrderBy(f=>f.ForecastLength)
            .ThenBy(f=>f.ForHour)
            .ToArray();

        var forecastRepo = ForecastHistoryRepo(forecastsBackInTime);
        var energyRepo = EnergyHistoryRepoHelper(24 * 7, energyAtRelevantTimes);

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

        var result = await svc.DetermineLatestForecastScalar(settings);

        Assert.Equal(1.0f, result.SunlightScalar);
    }}
