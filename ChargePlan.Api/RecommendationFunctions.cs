using System.Net;
using ChargePlan.Domain.Solver;
using ChargePlan.Service;
using ChargePlan.Service.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public Task<IActionResult> PostSolverRequestAdhoc([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "solver/requests/adhoc")] HttpRequest req)
        => req.CreateWithService<ChargePlanAdhocParameters, Recommendations>(_logger, nameof(PostSolverRequestAdhoc), _adhocService.CalculateRecommendations);

    [Function(nameof(PostSolverRequestMe))]
    public Task<IActionResult> PostSolverRequestMe([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "solver/requests/me")] HttpRequest req)
        => req.CreateWithService<UserRecommendationParameters, Recommendations>(_logger, nameof(PostSolverRequestMe), _userService.CalculateRecommendations);

    [Function(nameof(GetLastSolverRequestMe))]
    public Task<IActionResult> GetLastSolverRequestMe([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "solver/requests/me/last")] HttpRequest req)
        => req.GetFromService<Recommendations?>(_logger, nameof(GetLastSolverRequestMe), _userService.GetLastRecommendation);
}
