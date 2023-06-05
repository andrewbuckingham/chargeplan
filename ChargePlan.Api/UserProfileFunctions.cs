using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
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
    private readonly QueueServiceClient _queues;
    private readonly UserProfileService _service;

    public UserProfileFunctions(ILoggerFactory loggerFactory, QueueServiceClient queues, UserProfileService service)
    {
        _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
        _queues = queues ?? throw new ArgumentNullException(nameof(queues));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function(nameof(GetMyPlant))]
    public Task<HttpResponseData> GetMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/plant")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyPlant), _service.GetPlantParameters);

    [Function(nameof(PutMyPlant))]
    public Task<HttpResponseData> PutMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/me/plant")] HttpRequestData req)
        => req.UpdateWithService<UserPlantParameters>(_logger, nameof(PutMyPlant), _service.PutPlantParameters);

    [Function(nameof(PostCompletedDemandAsHash))]
    public Task<HttpResponseData> PostCompletedDemandAsHash([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/hashes")] HttpRequestData req)
        => req.CreateWithService<DemandCompleted, IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandAsHash), _service.PostCompletedDemandAsHash);

    [Function(nameof(PostCompletedDemandMatchFirstType))]
    public Task<HttpResponseData> PostCompletedDemandMatchFirstType([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/today/types")] HttpRequestData req)
        => req.CreateWithService<IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandMatchFirstType), _service.PostCompletedDemandMatchFirstType);

    [Function(nameof(GetCompletedDemandsToday))]
    public Task<HttpResponseData> GetCompletedDemandsToday([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/demands/completed/today/types")] HttpRequestData req)
        => req.CreateWithService<IEnumerable<DemandCompleted>>(_logger, nameof(GetCompletedDemandsToday), _service.GetCompletedDemandsToday);

    [Function(nameof(DeleteCompletedDemandToday))]
    public Task<HttpResponseData> DeleteCompletedDemandToday([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/me/demands/completed/today/types/{type}")] HttpRequestData req, string type)
        => req.DeleteWithService<IEnumerable<DemandCompleted>>(_logger, nameof(DeleteCompletedDemandToday), _service.DeleteCompletedDemandToday(type));
}
