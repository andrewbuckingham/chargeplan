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

        private readonly Guid _dummyUserId = Guid.Empty; // TODO: auth.

        public UserTemplateFunctions(ILoggerFactory loggerFactory, UserTemplateService service)
        {
            _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        private async Task<HttpResponseData> Get<T>(string name, HttpRequestData req, Func<Guid,Task<T>> service)
        {
            _logger.LogInformation(name);

            try
            {
                T result = await service(_dummyUserId) ?? throw new InvalidOperationException("Service returned null");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed calling service");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                return response;
            }
        }

        private async Task<HttpResponseData> Put<T>(string name, HttpRequestData req, Func<Guid,T,Task<T>> service)
        {
            _logger.LogInformation(name);

            try
            {
                T received = await req.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Client sent null");
                T result = await service(_dummyUserId, received) ?? throw new InvalidOperationException("Service returned null");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed calling service");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                return response;
            }
        }

        [Function(nameof(GetMyDemandProfiles))]
        public Task<HttpResponseData> GetMyDemandProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/demand/me")] HttpRequestData req)
            => Get(nameof(GetMyDemandProfiles), req, _service.GetDemandProfiles);

        [Function(nameof(PutMyDemandProfiles))]
        public Task<HttpResponseData> PutMyDemandProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/demand/me")] HttpRequestData req)
            => Put<IEnumerable<PowerAtAbsoluteTimes>>(nameof(PutMyDemandProfiles), req, _service.PutDemandProfiles);

        [Function(nameof(GetMyShiftableDemands))]
        public Task<HttpResponseData> GetMyShiftableDemands([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
            => Get(nameof(GetMyShiftableDemands), req, _service.GetShiftableDemands);

        [Function(nameof(PutMyShiftableDemands))]
        public Task<HttpResponseData> PutMyShiftableDemands([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/shiftabledemands/me")] HttpRequestData req)
            => Put<IEnumerable<PowerAtRelativeTimes>>(nameof(PutMyShiftableDemands), req, _service.PutShiftableDemands);

        [Function(nameof(GetMyChargeProfiles))]
        public Task<HttpResponseData> GetMyChargeProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/charge/me")] HttpRequestData req)
            => Get(nameof(GetMyChargeProfiles), req, _service.GetChargeProfiles);

        [Function(nameof(PutMyChargeProfiles))]
        public Task<HttpResponseData> PutMyChargeProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/charge/me")] HttpRequestData req)
            => Put<IEnumerable<PowerAtAbsoluteTimes>>(nameof(PutMyChargeProfiles), req, _service.PutChargeProfiles);

        [Function(nameof(GetMyPricingProfiles))]
        public Task<HttpResponseData> GetMyPricingProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/pricing/me")] HttpRequestData req)
            => Get(nameof(GetMyPricingProfiles), req, _service.GetPricingProfiles);

        [Function(nameof(PutMyPricingProfiles))]
        public Task<HttpResponseData> PutMyPricingProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/pricing/me")] HttpRequestData req)
            => Put<IEnumerable<PriceAtAbsoluteTimes>>(nameof(PutMyPricingProfiles), req, _service.PutPricingProfiles);

        [Function(nameof(GetMyExportProfiles))]
        public Task<HttpResponseData> GetMyExportProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/export/me")] HttpRequestData req)
            => Get(nameof(GetMyExportProfiles), req, _service.GetExportProfiles);

        [Function(nameof(PutMyExportProfiles))]
        public Task<HttpResponseData> PutMyExportProfiles([HttpTrigger(AuthorizationLevel.Function, "put", Route = "builder/templates/export/me")] HttpRequestData req)
            => Put<IEnumerable<PriceAtAbsoluteTimes>>(nameof(PutMyExportProfiles), req, _service.PutExportProfiles);
    }
}
