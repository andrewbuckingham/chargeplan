using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api
{
    public class DemandProfileFunctions
    {
        private readonly ILogger _logger;

        public DemandProfileFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DemandProfileFunctions>();
        }

        [Function(nameof(GetDemandProfiles))]
        public HttpResponseData GetDemandProfiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "builder/templates/me")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
