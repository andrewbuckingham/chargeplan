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

    [Function(nameof(GetMyPlantArrayShadingSimulationShaded))]
    public Task<HttpResponseData> GetMyPlantArrayShadingSimulationShaded([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/plant/arrayshading/simulation/shaded")] HttpRequestData req)
        => req.GetFromService(_logger, nameof(GetMyPlantArrayShadingSimulationShaded), _service.GetPlantArrayShadingSimulationShaded);

    [Function(nameof(PostCompletedDemandAsHash))]
    public Task<HttpResponseData> PostCompletedDemandAsHash([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/hashes")] HttpRequestData req)
        => req.CreateWithService<DemandCompleted, IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandAsHash), _service.PostCompletedDemandAsHash);

    [Function(nameof(PostCompletedDemandTodayType))]
    public Task<HttpResponseData> PostCompletedDemandTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/today/types/{type}")] HttpRequestData req, string type)
        => req.CreateWithService<IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandTodayType), (_) => _service.PostCompletedDemandTodayType(type));

    [Function(nameof(GetCompletedDemandsTodayType))]
    public Task<HttpResponseData> GetCompletedDemandsTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/demands/completed/today/types/{type}")] HttpRequestData req, string type)
        => req.GetFromService<IEnumerable<DemandCompleted>>(_logger, nameof(GetCompletedDemandsTodayType), () => _service.GetCompletedDemandsTodayType(type));

    [Function(nameof(DeleteCompletedDemandTodayType))]
    public Task<HttpResponseData> DeleteCompletedDemandTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/me/demands/completed/today/types/{type}")] HttpRequestData req, string type)
        => req.DeleteWithService<IEnumerable<DemandCompleted>>(_logger, nameof(DeleteCompletedDemandTodayType), _service.DeleteCompletedDemandTodayType(type));

#region Some overloads to help with clients that can't do DELETE verbs
    [Function(nameof(GetCompletedDemandsTodayType_BadHttpClient))]
    public Task<HttpResponseData> GetCompletedDemandsTodayType_BadHttpClient([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/demands/completed/today/types/{type}/delete")] HttpRequestData req, string type)
        => req.GetFromService<IEnumerable<DemandCompleted>>(_logger, nameof(GetCompletedDemandsTodayType_BadHttpClient), () => _service.GetCompletedDemandsTodayType(type));

    [Function(nameof(DeleteCompletedDemandTodayType_BadHttpClient))]
    public Task<HttpResponseData> DeleteCompletedDemandTodayType_BadHttpClient([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/today/types/{type}/delete")] HttpRequestData req, string type)
        => req.DeleteWithService<IEnumerable<DemandCompleted>>(_logger, nameof(DeleteCompletedDemandTodayType_BadHttpClient), _service.DeleteCompletedDemandTodayType(type));
#endregion
}
