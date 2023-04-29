using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public static class HttpRequestDataExtensions
{
    private static readonly Guid _dummyUserId = Guid.Empty; // TODO: auth.

    public static async Task<HttpResponseData> GetFromService<T>(this HttpRequestData req, ILogger logger, string name, Func<Guid, Task<T>> service)
    {
        logger.LogInformation(name);

        try
        {
            T result = await service(_dummyUserId) ?? throw new InvalidOperationException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed calling service");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            return response;
        }
    }

    public static async Task<HttpResponseData> UpdateWithService<T>(this HttpRequestData req, ILogger logger, string name, Func<Guid, T, Task<T>> service)
    {
        logger.LogInformation(name);

        try
        {
            T received = await req.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Client sent null");
            T result = await service(_dummyUserId, received) ?? throw new InvalidOperationException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed calling service");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            return response;
        }
    }

    public static async Task<HttpResponseData> CreateWithService<TParam, TResult>(this HttpRequestData req, ILogger logger, string name, Func<Guid, TParam, Task<TResult>> service)
    {
        logger.LogInformation(name);

        try
        {
            TParam received = await req.ReadFromJsonAsync<TParam>() ?? throw new InvalidOperationException("Client sent null");
            TResult result = await service(_dummyUserId, received) ?? throw new InvalidOperationException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed calling service");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            return response;
        }
    }
}