using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

public class AuthMiddleware : IFunctionsWorkerMiddleware
{
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