using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            HttpResponseData? response = context.GetHttpResponseData() ?? (await context.GetHttpRequestDataAsync())?.CreateResponse();
            HandleError(response, ex);

            if (response != null)
            {
                context.InvokeResult(response);
            }
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
            var statusCode = ex switch
            {
                NotPermittedException => HttpStatusCode.Forbidden,
                InvalidStateException => HttpStatusCode.UnprocessableEntity,
                JsonException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            _logger.LogError(ex, ex.ToString());

            if (response != null)
            {
                response.StatusCode = statusCode;
            }
        }

        return true;
    }
}