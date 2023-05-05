using System.Net;
using ChargePlan.Domain.Solver;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public class UserProfileFunctions
{
    private readonly ILogger _logger;
    private readonly UserProfileService _service;

    public UserProfileFunctions(ILoggerFactory loggerFactory, UserProfileService service)
    {
        _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function(nameof(GetMyPlant))]
    public Task<HttpResponseData> GetMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/plant")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyPlant), _service.GetPlantParameters);

    [Function(nameof(PutMyPlant))]
    public Task<HttpResponseData> PutMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/me/plant")] HttpRequestData req)
        => req.UpdateWithService<UserPlantParameters>(_logger, nameof(PutMyPlant), _service.PutPlantParameters);

    [Function(nameof(PostCompletedDemandAsHash))]
    public Task<HttpResponseData> PostCompletedDemandAsHash([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed")] HttpRequestData req)
        => req.CreateWithService<DemandCompleted, IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandAsHash), _service.PostCompletedDemandAsHash);
}
