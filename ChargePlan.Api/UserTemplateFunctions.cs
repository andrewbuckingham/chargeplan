using System.Net;
using ChargePlan.Builder.Templates;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public class UserTemplateFunctions
{
    private readonly ILogger _logger;
    private readonly UserTemplateService _service;

    public UserTemplateFunctions(ILoggerFactory loggerFactory, UserTemplateService service)
    {
        _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function(nameof(GetMyDemandProfiles))]
    public Task<HttpResponseData> GetMyDemandProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/demand/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyDemandProfiles), _service.GetDemandProfiles);

    [Function(nameof(PutMyDemandProfiles))]
    public Task<HttpResponseData> PutMyDemandProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/demand/me")] HttpRequestData req)
        => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyDemandProfiles), _service.PutDemandProfiles);

    [Function(nameof(GetMyShiftableDemands))]
    public Task<HttpResponseData> GetMyShiftableDemands([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyShiftableDemands), _service.GetShiftableDemands);

    [Function(nameof(PutMyShiftableDemands))]
    public Task<HttpResponseData> PutMyShiftableDemands([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
        => req.UpdateWithService<IEnumerable<PowerAtRelativeTimes>>(_logger, nameof(PutMyShiftableDemands), _service.PutShiftableDemands);

    [Function(nameof(GetMyChargeProfiles))]
    public Task<HttpResponseData> GetMyChargeProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/charge/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyChargeProfiles), _service.GetChargeProfiles);

    [Function(nameof(PutMyChargeProfiles))]
    public Task<HttpResponseData> PutMyChargeProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/charge/me")] HttpRequestData req)
        => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyChargeProfiles), _service.PutChargeProfiles);

    [Function(nameof(GetMyPricingProfiles))]
    public Task<HttpResponseData> GetMyPricingProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/pricing/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyPricingProfiles), _service.GetPricingProfiles);

    [Function(nameof(PutMyPricingProfiles))]
    public Task<HttpResponseData> PutMyPricingProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/pricing/me")] HttpRequestData req)
        => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyPricingProfiles), _service.PutPricingProfiles);

    [Function(nameof(GetMyExportProfiles))]
    public Task<HttpResponseData> GetMyExportProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/export/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyExportProfiles), _service.GetExportProfiles);

    [Function(nameof(PutMyExportProfiles))]
    public Task<HttpResponseData> PutMyExportProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/export/me")] HttpRequestData req)
        => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyExportProfiles), _service.PutExportProfiles);

    [Function(nameof(GetMyDayTemplates))]
    public Task<HttpResponseData> GetMyDayTemplates([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/days/me")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyDayTemplates), _service.GetDayTemplates);

    [Function(nameof(PutMyDayTemplates))]
    public Task<HttpResponseData> PutMyDayTemplates([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/days/me")] HttpRequestData req)
        => req.UpdateWithService<ChargePlanTemplatedParameters>(_logger, nameof(PutMyDayTemplates), _service.PutDayTemplates);
}
