using System.Net;
using ChargePlan.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api;

public static class HttpRequestDataExtensions
{
    private static async Task<IActionResult> WrapService(this HttpRequest req, ILogger logger, string name, Func<Task<IActionResult>> serviceCall)
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
            return new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogError(ex, $"Concurrency exception in service {name}");
            return new ConflictResult();
        }
        catch (InvalidStateException ex)
        {
            logger.LogError(ex, $"Invalid state exception in service {name}");
            return new UnprocessableEntityResult();
        }
        catch (NotAuthenticatedException ex)
        {
            logger.LogError(ex, $"Unauthenticated in service {name}");
            return new UnauthorizedResult();
        }
        catch (NotPermittedException ex)
        {
            logger.LogError(ex, $"Not permitted in service {name}");
            return new ForbidResult();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Failed calling service {name}");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }

    public static Task<IActionResult> GetFromService<T>(this HttpRequest req, ILogger logger, string name, Func<Task<T>> service)
        => req.WrapService(logger, name, async () => {
            T? result = await service();

            if (result == null) return new NotFoundResult();

            return new OkObjectResult(result);            
        });

    public static Task<IActionResult> UpdateWithService<T>(this HttpRequest req, ILogger logger, string name, Func<T, Task<T>> service)
        => req.WrapService(logger, name, async () => {
            T received = await req.ReadFromJsonAsync<T>() ?? throw new InvalidStateException("You must send some data");
            T result = await service(received) ?? throw new InvalidStateException("Service returned null");

            return new OkObjectResult(result);
        });
        
    public static Task<IActionResult> CreateWithService<TParam, TResult>(this HttpRequest req, ILogger logger, string name, Func<TParam, Task<TResult>> service)
        => req.WrapService(logger, name, async () => {
            TParam received = await req.ReadFromJsonAsync<TParam>() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");

            return new OkObjectResult(result);
        });

    public static Task<IActionResult> CreateWithService<TResult>(this HttpRequest req, ILogger logger, string name, Func<string, Task<TResult>> service)
        => req.WrapService(logger, name, async () => {
            using StreamReader r = new StreamReader(req.Body);
            string received = await r.ReadToEndAsync() ?? throw new InvalidStateException("You must send some data");
            TResult result = await service(received) ?? throw new InvalidStateException("Service returned null");

            return new OkObjectResult(result);
        });

    public static Task<IActionResult> DeleteWithService<TParam>(this HttpRequest req, ILogger logger, string name, Task service)
        => req.WrapService(logger, name, async () => {
            await service;
            return new OkResult();
        });
}