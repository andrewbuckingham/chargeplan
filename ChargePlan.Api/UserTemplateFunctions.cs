using System.Net;
using ChargePlan.Builder.Templates;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public Task<IActionResult> GetMyDemandProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/demand/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyDemandProfiles), _service.GetDemandProfiles);

    [Function(nameof(PutMyDemandProfiles))]
    public Task<IActionResult> PutMyDemandProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/demand/me")] HttpRequest req)
        => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyDemandProfiles), _service.PutDemandProfiles);

    [Function(nameof(GetMyShiftableDemands))]
    public Task<IActionResult> GetMyShiftableDemands([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/shiftabledemands/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyShiftableDemands), _service.GetShiftableDemands);

    [Function(nameof(PutMyShiftableDemands))]
    public Task<IActionResult> PutMyShiftableDemands([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/shiftabledemands/me")] HttpRequest req)
        => req.UpdateWithService<IEnumerable<PowerAtRelativeTimes>>(_logger, nameof(PutMyShiftableDemands), _service.PutShiftableDemands);

    [Function(nameof(GetMyChargeProfiles))]
    public Task<IActionResult> GetMyChargeProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/charge/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyChargeProfiles), _service.GetChargeProfiles);

    [Function(nameof(PutMyChargeProfiles))]
    public Task<IActionResult> PutMyChargeProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/charge/me")] HttpRequest req)
        => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyChargeProfiles), _service.PutChargeProfiles);

    [Function(nameof(GetMyPricingProfiles))]
    public Task<IActionResult> GetMyPricingProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/pricing/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyPricingProfiles), _service.GetPricingProfiles);

    [Function(nameof(PutMyPricingProfiles))]
    public Task<IActionResult> PutMyPricingProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/pricing/me")] HttpRequest req)
        => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyPricingProfiles), _service.PutPricingProfiles);

    [Function(nameof(GetMyExportProfiles))]
    public Task<IActionResult> GetMyExportProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/export/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyExportProfiles), _service.GetExportProfiles);

    [Function(nameof(PutMyExportProfiles))]
    public Task<IActionResult> PutMyExportProfiles([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/export/me")] HttpRequest req)
        => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyExportProfiles), _service.PutExportProfiles);

    [Function(nameof(GetMyDayTemplates))]
    public Task<IActionResult> GetMyDayTemplates([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/days/me")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyDayTemplates), _service.GetDayTemplates);

    [Function(nameof(PutMyDayTemplates))]
    public Task<IActionResult> PutMyDayTemplates([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/days/me")] HttpRequest req)
        => req.UpdateWithService<ChargePlanTemplatedParameters>(_logger, nameof(PutMyDayTemplates), _service.PutDayTemplates);

    [Function(nameof(GetMyDayTemplateTomorrowDemand))]
    public Task<IActionResult> GetMyDayTemplateTomorrowDemand([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/days/me/tomorrow")] HttpRequest req)
        => req.GetFromService<DayTemplate>(_logger, nameof(GetMyDayTemplateTomorrowDemand), _service.GetTomorrowsDemand);

    [Function(nameof(GetMyDayTemplateTodayDemand))]
    public Task<IActionResult> GetMyDayTemplateTodayDemand([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "builder/templates/days/me/today")] HttpRequest req)
        => req.GetFromService<DayTemplate>(_logger, nameof(GetMyDayTemplateTodayDemand), _service.GetTodaysDemand);

    [Function(nameof(PutMyDayTemplateTomorrowDemand))]
    public Task<IActionResult> PutMyDayTemplateTomorrowDemand([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/days/me/tomorrow")] HttpRequest req)
        => req.UpdateWithService<DayTemplate>(_logger, nameof(PutMyDayTemplateTomorrowDemand), _service.PutTomorrowsDemand);

    [Function(nameof(PutMyDayTemplateTodayDemand))]
    public Task<IActionResult> PutMyDayTemplateTodayDemand([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "builder/templates/days/me/today")] HttpRequest req)
        => req.UpdateWithService<DayTemplate>(_logger, nameof(PutMyDayTemplateTodayDemand), _service.PutTodaysDemand);
}
