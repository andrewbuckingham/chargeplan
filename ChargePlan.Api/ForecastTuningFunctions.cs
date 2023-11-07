using ChargePlan.Service;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Entities.ForecastTuning;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public class ForecastTuningFunctions
{
    private const string MyUserId = "4056aeae-03e5-4c9d-bd30-d2e3771f971f";

    private readonly ILogger _logger;
    private readonly ForecastTuningService _service;

    public ForecastTuningFunctions(ILoggerFactory loggerFactory, ForecastTuningService service)
    {
        _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function(nameof(StoreForecastHistory))]
    public Task StoreForecastHistory([TimerTrigger("0 * * * *")]TimerInfo myTimer)
        => _service.StoreForecastInHistory(new Guid(MyUserId));

    [Function(nameof(StoreEnergyHistory))]
    public Task<HttpResponseData> StoreEnergyHistory([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/forecast/energyhistory")] HttpRequestData req)
        => req.CreateWithService<IEnumerable<EnergyDatapoint>, IEnumerable<EnergyDatapoint>>(_logger, nameof(StoreEnergyHistory), _service.StoreEnergyInHistory);

    [Function(nameof(DetermineLatestForecastScalar))]
    public Task<HttpResponseData> DetermineLatestForecastScalar([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/forecast/latestscalar")] HttpRequestData req)
        => req.GetFromService<WeatherForecastSettings>(_logger, nameof(DetermineLatestForecastScalar), () => _service.DetermineLatestForecastScalar());

    [Function(nameof(DetermineLatestForecastScalarForRange))]
    public Task<HttpResponseData> DetermineLatestForecastScalarForRange([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/forecast/latestscalar/periods/days/{days}")] HttpRequestData req, int days)
        => req.GetFromService<WeatherForecastSettings>(_logger, nameof(DetermineLatestForecastScalarForRange), ()
        => _service.DetermineLatestForecastScalar(new ForecastTuningSettings() with { PeriodToAverageOver = TimeSpan.FromDays(days) }));

    [Function(nameof(DetermineAndApplyLatestForecastScalar))]
    public Task<HttpResponseData> DetermineAndApplyLatestForecastScalar([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/forecast/latestscalar")] HttpRequestData req)
        => req.CreateWithService<WeatherForecastSettings>(_logger, nameof(DetermineAndApplyLatestForecastScalar), (_) => _service.DetermineAndApplyLatestForecastScalar());

    [Function(nameof(DetermineAndApplyLatestForecastScalarForRange))]
    public Task<HttpResponseData> DetermineAndApplyLatestForecastScalarForRange([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/forecast/latestscalar/periods/days/{days}")] HttpRequestData req, int days)
        => req.CreateWithService<WeatherForecastSettings>(_logger, nameof(DetermineAndApplyLatestForecastScalarForRange), (_)
        => _service.DetermineAndApplyLatestForecastScalar(new ForecastTuningSettings() with { PeriodToAverageOver = TimeSpan.FromDays(days) }));
}