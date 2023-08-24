using ChargePlan.Service;
using Microsoft.Azure.Functions.Worker;
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
}