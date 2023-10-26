using System.Net;
using ChargePlan.Domain.Exceptions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public static class HttpRequestDataExtensions
{
    private static async Task<HttpResponseData> WrapService(this HttpRequestData req, ILogger logger, string name, Func<Task<HttpResponseData>> serviceCall)
    {
        logger.LogInformation($"Starting {name}");
        try
        {
            var response = await serviceCall();
            return response;
        }
        catch (InfrastructureException ex)
        {
            logger.LogError(ex, $"Infrastructure exception in service {name}");
            return req.CreateResponse(HttpStatusCode.ServiceUnavailable);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogError(ex, $"Concurrency exception in service {name}");
            return req.CreateResponse(HttpStatusCode.Conflict);
        }
        catch (InvalidStateException ex)
        {
            logger.LogError(ex, $"Invalid state exception in service {name}");
            return req.CreateResponse(HttpStatusCode.UnprocessableEntity);
        }
        catch (NotAuthenticatedException ex)
        {
            logger.LogError(ex, $"Unauthenticated in service {name}");
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }
        catch (NotPermittedException ex)
        {
            logger.LogError(ex, $"Not permitted in service {name}");
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Failed calling service {name}");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    public static Task<HttpResponseData> GetFromService<T>(this HttpRequestData req, ILogger logger, string name, Func<Task<T>> service)
        => req.WrapService(logger, name, async () => {
            T? result = await service();

            if (result == null) return req.CreateResponse(HttpStatusCode.NotFound);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        });

    public static Task<HttpResponseData> UpdateWithService<T>(this HttpRequestData req, ILogger logger, string name, Func<T, Task<T>> service)
        => req.WrapService(logger, name, async () => {
            T received = await req.ReadFromJsonAsync<T>() ?? throw new InvalidStateException("You must send some data");
            T result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        });
        
    public static Task<HttpResponseData> CreateWithService<TParam, TResult>(this HttpRequestData req, ILogger logger, string name, Func<TParam, Task<TResult>> service)
        => req.WrapService(logger, name, async () => {
            TParam received = await req.ReadFromJsonAsync<TParam>() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        });

    public static Task<HttpResponseData> CreateWithService<TResult>(this HttpRequestData req, ILogger logger, string name, Func<string, Task<TResult>> service)
        => req.WrapService(logger, name, async () => {
            string received = await req.ReadAsStringAsync() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        });

    public static Task<HttpResponseData> DeleteWithService<TParam>(this HttpRequestData req, ILogger logger, string name, Task service)
        => req.WrapService(logger, name, async () => {
            await service;
            var response = req.CreateResponse(HttpStatusCode.OK);

            return response;
        });
}