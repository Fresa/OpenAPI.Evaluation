using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace OpenAPI.Validation;

public sealed class OpenApiOperation
{
    private readonly RoutePattern _routePattern;
    private readonly OpenApiEvaluationContext _operationEvaluationContext;

    internal OpenApiOperation(
        RoutePattern routePattern,
        OpenApiEvaluationContext operationEvaluationContext)
    {
        _routePattern = routePattern;
        _operationEvaluationContext = operationEvaluationContext;
    }
    
    public bool TryGetResponseSpecification(HttpStatusCode statusCode, [NotNullWhen(true)] out OpenApiOperationResponse? response)
    {
        var responsesReader = _operationEvaluationContext.Evaluate("responses");
        if (responsesReader.TryEvaluate(PointerSegment.Create(((int)statusCode).ToString()),
                out var responseEvaluationContext))
        {
            response = new OpenApiOperationResponse(responseEvaluationContext);
            return true;
        }

        response = null;
        return false;
    }

    public void EvaluateRequestContent(JsonNode? content)
    {
        if (!_operationEvaluationContext.TryEvaluate("requestBody", out var requestEvaluationContext))
        {
            return;
        }

        var requestBodySchemaEvaluationResults = requestEvaluationContext.Evaluate("content", "application/json", "schema");
        requestBodySchemaEvaluationResults.EvaluateAgainstSchema(content);
    }

    public void EvaluateRequestQueryParameters(Uri? uri)
    {
        var querystring = uri?.Query;
        if (string.IsNullOrEmpty(querystring))
        {
            return;
        }
        var queryParameters = System.Web.HttpUtility.ParseQueryString(querystring);

        var parametersEvaluationContext = _operationEvaluationContext.Evaluate("parameters");
        foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
        {
            if (parameterEvaluationContext.GetValue<string>("in") != "query")
                continue;

            var name = parameterEvaluationContext.GetValue<string>("name");
            var stringValues = queryParameters.GetValues(name);
            if (stringValues == null)
            {
                if (parameterEvaluationContext.TryGetValue<bool>("required", out var required) &&
                    required)
                {
                    parameterEvaluationContext.EvaluateAsRequired(name);
                }
                continue;
            }

            var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
            parameterSchemaEvaluationResults.EvaluateAgainstSchema(stringValues);
        }
    }

    public void EvaluateRequestPathParameters()
    {
        var parametersEvaluationContext = _operationEvaluationContext.Evaluate("parameters");
        foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
        {
            if (parameterEvaluationContext.GetValue<string>("in") != "path")
                continue;

            var name = parameterEvaluationContext.GetValue<string>("name");
            if (!_routePattern.Values.TryGetValue(name, out var routeValue))
            {
                throw new InvalidOperationException(
                    $"The endpoint {_routePattern.Template} is invalid as it does not contain the defined path parameter {name} specified at {parameterEvaluationContext.Results.SpecificationLocation}");
            }

            var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
            var value = JsonValue.Create(routeValue);
            parameterSchemaEvaluationResults.EvaluateAgainstSchema(value);
        }
    }

    public void EvaluateRequestHeaders(HttpRequestHeaders headers)
    {
        var parametersEvaluationContext = _operationEvaluationContext.Evaluate("parameters");
        foreach (var parameterEvaluationContext in parametersEvaluationContext.EvaluateChildren())
        {
            if (parameterEvaluationContext.GetValue<string>("in") != "header")
                continue;

            var name = parameterEvaluationContext.GetValue<string>("name");
            if (!headers.TryGetValues(name, out var stringValues))
            {
                if (parameterEvaluationContext.TryGetValue<bool>("required", out var required) &&
                    required)
                {
                    parameterEvaluationContext.EvaluateAsRequired(name);
                }
                continue;
            }

            var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
            parameterSchemaEvaluationResults.EvaluateAgainstSchema(stringValues.ToArray());
        }
    }

    public OpenApiEvaluationResults GetEvaluationResults() => _operationEvaluationContext.Results;
}