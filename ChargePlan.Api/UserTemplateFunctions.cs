using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api
{
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
        public Task<HttpResponseData> GetMyDemandProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/demand/me")] HttpRequestData req)
            => req.GetFromService(_logger, nameof(GetMyDemandProfiles), _service.GetDemandProfiles);

        [Function(nameof(PutMyDemandProfiles))]
        public Task<HttpResponseData> PutMyDemandProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/demand/me")] HttpRequestData req)
            => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyDemandProfiles), _service.PutDemandProfiles);

        [Function(nameof(GetMyShiftableDemands))]
        public Task<HttpResponseData> GetMyShiftableDemands([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
            => req.GetFromService(_logger, nameof(GetMyShiftableDemands), _service.GetShiftableDemands);

        [Function(nameof(PutMyShiftableDemands))]
        public Task<HttpResponseData> PutMyShiftableDemands([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
            => req.UpdateWithService<IEnumerable<PowerAtRelativeTimes>>(_logger, nameof(PutMyShiftableDemands), _service.PutShiftableDemands);

        [Function(nameof(GetMyChargeProfiles))]
        public Task<HttpResponseData> GetMyChargeProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/charge/me")] HttpRequestData req)
            => req.GetFromService(_logger, nameof(GetMyChargeProfiles), _service.GetChargeProfiles);

        [Function(nameof(PutMyChargeProfiles))]
        public Task<HttpResponseData> PutMyChargeProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/charge/me")] HttpRequestData req)
            => req.UpdateWithService<IEnumerable<PowerAtAbsoluteTimes>>(_logger, nameof(PutMyChargeProfiles), _service.PutChargeProfiles);

        [Function(nameof(GetMyPricingProfiles))]
        public Task<HttpResponseData> GetMyPricingProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/pricing/me")] HttpRequestData req)
            => req.GetFromService(_logger, nameof(GetMyPricingProfiles), _service.GetPricingProfiles);

        [Function(nameof(PutMyPricingProfiles))]
        public Task<HttpResponseData> PutMyPricingProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/pricing/me")] HttpRequestData req)
            => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyPricingProfiles), _service.PutPricingProfiles);

        [Function(nameof(GetMyExportProfiles))]
        public Task<HttpResponseData> GetMyExportProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/export/me")] HttpRequestData req)
            => req.GetFromService(_logger, nameof(GetMyExportProfiles), _service.GetExportProfiles);

        [Function(nameof(PutMyExportProfiles))]
        public Task<HttpResponseData> PutMyExportProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/export/me")] HttpRequestData req)
            => req.UpdateWithService<IEnumerable<PriceAtAbsoluteTimes>>(_logger, nameof(PutMyExportProfiles), _service.PutExportProfiles);
    }
}
