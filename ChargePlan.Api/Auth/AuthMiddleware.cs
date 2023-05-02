using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

public class AuthMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        (Type featureType, object featureInstance) = context.Features.SingleOrDefault(x => x.Key.Name == "IFunctionBindingsFeature");

        var inputData = featureType.GetProperties().SingleOrDefault(p => p.Name == "InputData")?.GetValue(featureInstance) as IReadOnlyDictionary<string, object>;
        var requestData = inputData?.Values.SingleOrDefault(obj => obj is HttpRequestData) as HttpRequestData;

        IEnumerable<string>? apiKeyValues;
        if (requestData?.Headers.TryGetValues("api-key", out apiKeyValues) == true)
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