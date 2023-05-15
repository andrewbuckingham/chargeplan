using System.Net;
using ChargePlan.Domain.Exceptions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public static class HttpRequestDataExtensions
{
    public static async Task<HttpResponseData> GetFromService<T>(this HttpRequestData req, ILogger logger, string name, Func<Task<T>> service)
    {
        logger.LogInformation(name);

        try
        {
            T? result = await service(); //?? throw new InvalidStateException("Service returned null");

            if (result == null) return req.CreateResponse(HttpStatusCode.NotFound);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed calling service {name}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            //return response;
            throw;
        }
    }

    public static async Task<HttpResponseData> UpdateWithService<T>(this HttpRequestData req, ILogger logger, string name, Func<T, Task<T>> service)
    {
        logger.LogInformation(name);

        try
        {
            T received = await req.ReadFromJsonAsync<T>() ?? throw new InvalidStateException("You must send some data");
            T result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {            
            logger.LogError(ex, $"Failed calling service {name}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            //return response;
            throw;
        }
    }

    public static async Task<HttpResponseData> CreateWithService<TParam, TResult>(this HttpRequestData req, ILogger logger, string name, Func<TParam, Task<TResult>> service)
    {
        logger.LogInformation(name);

        try
        {
            TParam received = await req.ReadFromJsonAsync<TParam>() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed calling service {name}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            //return response;
            throw;
        }
    }

    public static async Task<HttpResponseData> CreateWithService<TResult>(this HttpRequestData req, ILogger logger, string name, Func<string, Task<TResult>> service)
    {
        logger.LogInformation(name);

        try
        {
            string received = await req.ReadAsStringAsync() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed calling service {name}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            //return response;
            throw;
        }
    }
}