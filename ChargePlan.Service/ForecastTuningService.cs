using System.Runtime.CompilerServices;
using System.Text.Json;
using ChargePlan.Domain;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Entities.ForecastTuning;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;
using ChargePlan.Weather;
using Microsoft.Extensions.Logging;
using Polly;

namespace ChargePlan.Service;

/// <summary>
/// Helps with finding the optimum PV scalar for the forecast, by looking at the history of
/// forecast data, and the history of actual PV output.
/// </summary>
public class ForecastTuningService
{
    private readonly ILogger _logger;

    private readonly UserPermissionsFacade _user;
    private readonly IDirectNormalIrradianceProvider _dniWeatherProvider;
    private readonly IForecastHistoryRepository _forecastHistoryRepository;
    private readonly IEnergyHistoryRepository _energyHistoryRepository;
    private readonly IInterpolationFactory _interpolationFactory;

    private readonly IUserRepositories _repos;

    public ForecastTuningService(ILogger<ForecastTuningService> logger,
        UserPermissionsFacade user,
        IDirectNormalIrradianceProvider dniWeatherProvider,
        IForecastHistoryRepository forecastHistoryRepository,
        IEnergyHistoryRepository energyHistoryRepository,
        IInterpolationFactory interpolationFactory,
        IUserRepositories repos)
    {
        _logger = logger;
        _user = user;
        _dniWeatherProvider = dniWeatherProvider;
        _interpolationFactory = interpolationFactory;
        _forecastHistoryRepository = forecastHistoryRepository;
        _energyHistoryRepository = energyHistoryRepository;

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

        DateTimeOffset start = DateTimeOffset.Now.ToClosestHour();
        DateTimeOffset forecastFor = start;
        TimeSpan step = TimeSpan.FromHours(1);
        while (forecastFor <= start + ForecastTuningSettings.MaximumForecastLengthToStore)
        {
            double from = (forecastFor).AsTotalHours();
            double to = (forecastFor + step).AsTotalHours();
            float energy = Math.Max(0.0f, (float)forecastSpline.Integrate(from, to));

            history.Entity.Values.Add(new ForecastDatapoint(
                ForHour: forecastFor,
                ProducedAt: start,
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

        _logger.LogInformation($"Added forecast history at {start}");
    }

    public async Task<IEnumerable<EnergyDatapoint>> StoreEnergyInHistory(IEnumerable<EnergyDatapoint> energyDatapoints)
    {
        var history = await _energyHistoryRepository.GetAsync(_user.Id) ?? new(new(), String.Empty);

        // Ensure aligned to closest hour.
        energyDatapoints = energyDatapoints.Select(f=>f with { InHour = f.InHour.ToClosestHour() });

        // New datapoints overwrite old ones from the same hour.
        var newDataTimestamps = energyDatapoints.Select(f=>f.InHour).ToHashSet();
        history.Entity.Values = history.Entity.Values.Where(f=>newDataTimestamps.Contains(f.InHour) == false).ToList();
        history.Entity.Values.AddRange(energyDatapoints);

        await _energyHistoryRepository.UpsertAsync(_user.Id, history);        

        _logger.LogInformation($"Added {energyDatapoints.Count()} datapoints to energy history");

        return energyDatapoints;
    }

    public async Task<WeatherForecastSettings> DetermineLatestForecastScalar(ForecastTuningSettings? settings = null)
    {
        settings ??= new();

        var forecastHistory = (await _forecastHistoryRepository.GetAsync(_user.Id))?.Entity ?? new();
        var energyHistory = (await _energyHistoryRepository.GetAsync(_user.Id))?.Entity ?? new();

        var now = DateTimeOffset.Now.ToClosestHour();

        var forecasts = forecastHistory
            .GetHourlyForecastsForHorizon(settings.ForecastLengthToOptimiseFor)
            .Where(f=>f.ForHour > now - settings.PeriodToAverageOver)
            .ToDictionary(f=>f.ForHour);
        
        var actuals = energyHistory.Values
            .Where(f=>f.InHour > now - settings.PeriodToAverageOver)
            .Where(f=>f.InHour < now.AddHours(-1) && f.Energy >= settings.IgnoreEnergiesBelow) // Most recent data could be partial so ignore. Likewise small values.
            .ToDictionary(f=>f.InHour);

        // Match forecast and actuals.
        var joined = forecasts
            .Join(actuals, f=>f.Key, f=>f.Key, (forecast,actual) => (DateTime: forecast.Key, Forecast: forecast.Value, Actual: actual.Value))
            .ToArray();

        if (joined.Count() < 8) throw new InvalidStateException($"Too few datapoints for determining forecast scalar. Found {joined.Count()} but need at least 8.");

        float totalForecasted = joined.Sum(f=>f.Forecast.Energy);
        float totalActual = joined.Sum(f=>f.Actual.Energy);

        if (totalForecasted < 1.0f) throw new InvalidStateException("Total forecasted energy is too small for determining forecast scalar.");
        if (totalActual < 1.0f) throw new InvalidStateException("Total actual energy is too small for determining forecast scalar.");

        float scalar = totalActual / totalForecasted;

        return new WeatherForecastSettings(scalar, scalar);
    }

    public async Task<WeatherForecastSettings> DetermineAndApplyLatestForecastScalar(ForecastTuningSettings? settings = null)
    {
        var weather = await DetermineLatestForecastScalar(settings);

        var plant = await _repos.Plant.GetAsync(_user.Id) ?? throw new InvalidStateException("No plant information exists for user");
        plant = plant with { WeatherForecastSettings = weather };
        await _repos.Plant.UpsertAsync(_user.Id, plant);

        return weather;
    }
}
