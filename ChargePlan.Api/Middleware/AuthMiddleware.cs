using ChargePlan.Api.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api.Middleware;

public class AuthMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger;

    public AuthMiddleware(ILogger<AuthMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();

        IEnumerable<string>? apiKeyValues;
        if (requestData?.Headers.TryGetValues("Api-Key", out apiKeyValues) == true)
        {
            string? apiKey = apiKeyValues?.SingleOrDefault();
            if (apiKey != null && Guid.TryParse(apiKey, out _) == true)
            {
                var userId = new Guid(apiKey);
                if (userId != Guid.Empty)
                {
                    var accessor = context.InstanceServices.GetRequiredService<UserIdAccessor>();
                    accessor.UserId = userId;
                }
            }
        }

        await next(context);
    }
}