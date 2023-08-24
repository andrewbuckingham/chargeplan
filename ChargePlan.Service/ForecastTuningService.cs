using System.Runtime.CompilerServices;
using System.Text.Json;
using ChargePlan.Domain;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;
using ChargePlan.Weather;
using Microsoft.Extensions.Logging;
using Polly;

namespace ChargePlan.Service;

public class ForecastTuningService
{
    private readonly ILogger _logger;

    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IForecastHistoryRepository _forecastHistoryRepository;
    private readonly IInterpolationFactory _interpolationFactory;

    private readonly IUserRepositories _repos;

    private static readonly TimeSpan MaximumForecastLengthToStore = TimeSpan.FromHours(24);

    public ForecastTuningService(ILogger<ForecastTuningService> logger,
        IDirectNormalIrradianceProvider dniWeatherProvider,
        IForecastHistoryRepository forecastHistoryRepository,
        IInterpolationFactory interpolationFactory,
        IUserRepositories repos)
    {
        _logger = logger;
        _dniWeatherProvider = dniWeatherProvider;
        _interpolationFactory = interpolationFactory;
        _forecastHistoryRepository = forecastHistoryRepository;

        _repos = repos;
    }


    /// <summary>
    /// Store the current forecast data so that its efficacy can be evaluated later.
    /// Recommend calling this periodically e.g. once per hour.
    /// </summary>
    public async Task StoreForecastInHistory(Guid userId)
    {
        var plantSpec = await _repos.Plant.GetAsync(userId) ?? new();

        var forecastSpline = (await new WeatherBuilder(
                plantSpec.ArraySpecification.ArrayElevationDegrees,
                plantSpec.ArraySpecification.ArrayAzimuthDegrees,
                plantSpec.ArraySpecification.LatDegrees,
                plantSpec.ArraySpecification.LongDegrees)
            .WithArrayArea(plantSpec.ArraySpecification.ArrayArea, absolutePeakWatts: plantSpec.ArraySpecification.AbsolutePeakWatts)
            .WithDniSource(_dniWeatherProvider)
            .AddShading(plantSpec.ArrayShading)
            .BuildAsync())
            .AsSpline(_interpolationFactory.InterpolateGeneration);

        var history = await _forecastHistoryRepository.GetAsync(userId) ?? new(new(), String.Empty);

        DateTimeOffset forecastFor = DateTimeOffset.Now.ToClosestHour();
        TimeSpan step = TimeSpan.FromHours(1);
        while (forecastFor <= forecastFor + MaximumForecastLengthToStore)
        {
            double from = (forecastFor).AsTotalHours();
            double to = (forecastFor + step).AsTotalHours();
            float energy = Math.Max(0.0f, (float)forecastSpline.Integrate(from, to));

            history.Entity.Values.Add(new ForecastDatapoint(
                ForHour: forecastFor,
                ProducedAt: DateTimeOffset.Now,
                Energy: energy,
                CloudCoverPercent: 0 // TODO.
            ));

            forecastFor += step;
        }

        history.Entity.Values = history.Entity.Values
            .OrderBy(f=>f.ForHour)
            .ThenBy(f=>f.ForecastLength)
            .ToList();

        await _forecastHistoryRepository.UpsertAsync(userId, history);
    }
}
