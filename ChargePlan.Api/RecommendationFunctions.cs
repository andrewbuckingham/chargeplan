using System.Net;
using ChargePlan.Domain.Solver;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public class RecommendationFunctions
{
    private readonly ILogger _logger;
    private readonly AdhocRecommendationService _adhocService;
    private readonly UserRecommendationService _userService;

    public RecommendationFunctions(ILoggerFactory loggerFactory, AdhocRecommendationService adhocService, UserRecommendationService userService)
    {
        _logger = loggerFactory.CreateLogger<UserTemplateFunctions>();
        _adhocService = adhocService ?? throw new ArgumentNullException(nameof(adhocService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    [Function(nameof(PostSolverRequestAdhoc))]
    public Task<HttpResponseData> PostSolverRequestAdhoc([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "solver/requests/adhoc")] HttpRequestData req)
        => req.CreateWithService<ChargePlanAdhocParameters, Recommendations>(_logger, nameof(PostSolverRequestAdhoc), _adhocService.CalculateRecommendations);



    [Function(nameof(PostSolverRequestMe))]
    public Task<HttpResponseData> PostSolverRequestMe([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "solver/requests/me")] HttpRequestData req)
        => req.CreateWithService<UserRecommendationParameters, Recommendations>(_logger, nameof(PostSolverRequestMe), _userService.CalculateRecommendations);
}
