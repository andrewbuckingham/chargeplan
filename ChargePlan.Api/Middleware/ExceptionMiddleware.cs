using System.Net;
using System.Text.Json;
using ChargePlan.Domain.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChargePlan.Api.Middleware;

public class ExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);    
        }
        catch (Exception ex)
        {
            var response = (await context.GetHttpRequestDataAsync())?.CreateResponse();
            HandleError(response, ex);
            context.GetInvocationResult().Value = response;
        }
    }

    private bool HandleError(HttpResponseData? response, Exception ex)
    {
        if (ex is AggregateException ae)
        {
            ae.Handle(innerException => HandleError(response, innerException));
        }
        else
        {
            var (statusCode, logLevel) = ex switch
            {
                NotAuthenticatedException => (HttpStatusCode.Unauthorized, LogLevel.Error),
                NotPermittedException => (HttpStatusCode.Forbidden, LogLevel.Warning),
                InvalidStateException => (HttpStatusCode.UnprocessableEntity, LogLevel.Warning),
                JsonException => (HttpStatusCode.BadRequest, LogLevel.Warning),
                _ => (HttpStatusCode.InternalServerError, LogLevel.Error)
            };

            _logger.Log(logLevel, ex, ex.ToString());

            if (response != null)
            {
                response.StatusCode = statusCode;
            }
        }

        return true;
    }
}