using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api
{
    public class RecommendationFunctions
    {
        private readonly ILogger _logger;
        private readonly RecommendationService _service;

        public RecommendationFunctions(ILoggerFactory loggerFactory, RecommendationService service)
        {
            _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [Function(nameof(PostSolverRequestAdhoc))]
        public Task<HttpResponseData> PostSolverRequestAdhoc([HttpTrigger(AuthorizationLevel.Function, "post", Route = "solver/requests/adhoc")] HttpRequestData req)
            => req.CreateWithService<ChargePlanAdhocParameters, Recommendations>(_logger, nameof(PostSolverRequestAdhoc), _service.CalculateRecommendations);
    }
}
