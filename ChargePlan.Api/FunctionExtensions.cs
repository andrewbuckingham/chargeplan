using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

internal static class FunctionExtensions
{
    internal static HttpRequestData? GetHttpRequestData(this FunctionContext context)
    {
        var keyValuePair = context.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
        var functionBindingsFeature = keyValuePair.Value;
        var type = functionBindingsFeature.GetType();
        var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
        return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
    }

    internal static void InvokeResult(this FunctionContext context, HttpResponseData response)
    {
        var keyValuePair = context.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
        var functionBindingsFeature = keyValuePair.Value;
        var type = functionBindingsFeature.GetType();
        var result = type.GetProperties().Single(p => p.Name == "InvocationResult");
        result.SetValue(functionBindingsFeature, response);
    }
}