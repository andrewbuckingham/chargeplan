using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using ChargePlan.Domain.Solver;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public Task<IActionResult> GetMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/plant")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyPlant), _service.GetPlantParameters);

    [Function(nameof(PutMyPlant))]
    public Task<IActionResult> PutMyPlant([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/me/plant")] HttpRequest req)
        => req.UpdateWithService<UserPlantParameters>(_logger, nameof(PutMyPlant), _service.PutPlantParameters);

    [Function(nameof(GetMyPlantArrayShadingSimulationShaded))]
    public Task<IActionResult> GetMyPlantArrayShadingSimulationShaded([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/plant/arrayshading/simulation/shaded")] HttpRequest req)
        => req.GetFromService(_logger, nameof(GetMyPlantArrayShadingSimulationShaded), _service.GetPlantArrayShadingSimulationShaded);

    [Function(nameof(PostCompletedDemandAsHash))]
    public Task<IActionResult> PostCompletedDemandAsHash([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/hashes")] HttpRequest req)
        => req.CreateWithService<DemandCompleted, IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandAsHash), _service.PostCompletedDemandAsHash);

    [Function(nameof(PostCompletedDemandTodayType))]
    public Task<IActionResult> PostCompletedDemandTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/today/types/{type}")] HttpRequest req, string type)
        => req.CreateWithService<IEnumerable<DemandCompleted>>(_logger, nameof(PostCompletedDemandTodayType), (_) => _service.PostCompletedDemandTodayType(type));

    [Function(nameof(GetCompletedDemandsTodayType))]
    public Task<IActionResult> GetCompletedDemandsTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/demands/completed/today/types/{type}")] HttpRequest req, string type)
        => req.GetFromService<IEnumerable<DemandCompleted>>(_logger, nameof(GetCompletedDemandsTodayType), () => _service.GetCompletedDemandsTodayType(type));

    [Function(nameof(DeleteCompletedDemandTodayType))]
    public Task<IActionResult> DeleteCompletedDemandTodayType([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/me/demands/completed/today/types/{type}")] HttpRequest req, string type)
        => req.DeleteWithService<IEnumerable<DemandCompleted>>(_logger, nameof(DeleteCompletedDemandTodayType), _service.DeleteCompletedDemandTodayType(type));

#region Some overloads to help with clients that can't do DELETE verbs
    [Function(nameof(GetCompletedDemandsTodayType_BadHttpClient))]
    public Task<IActionResult> GetCompletedDemandsTodayType_BadHttpClient([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me/demands/completed/today/types/{type}/delete")] HttpRequest req, string type)
        => req.GetFromService<IEnumerable<DemandCompleted>>(_logger, nameof(GetCompletedDemandsTodayType_BadHttpClient), () => _service.GetCompletedDemandsTodayType(type));

    [Function(nameof(DeleteCompletedDemandTodayType_BadHttpClient))]
    public Task<IActionResult> DeleteCompletedDemandTodayType_BadHttpClient([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/me/demands/completed/today/types/{type}/delete")] HttpRequest req, string type)
        => req.DeleteWithService<IEnumerable<DemandCompleted>>(_logger, nameof(DeleteCompletedDemandTodayType_BadHttpClient), _service.DeleteCompletedDemandTodayType(type));
#endregion
}
