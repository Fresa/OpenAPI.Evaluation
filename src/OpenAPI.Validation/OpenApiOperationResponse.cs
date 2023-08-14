using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation;

public sealed class OpenApiOperationResponse
{
    private readonly OpenApiEvaluationContext _responseEvaluationContext;
    
    internal OpenApiOperationResponse(
        OpenApiEvaluationContext responseEvaluationContext)
    {
        _responseEvaluationContext = responseEvaluationContext;
    }
    
    public void EvaluateContent(JsonNode? content)
    {
        if (!TryGetSchemaEvaluationContext(out var schemaEvaluationContext))
            return;

        schemaEvaluationContext.EvaluateAgainstSchema(content);
    }

    private bool TryGetSchemaEvaluationContext(
        [NotNullWhen(true)] out OpenApiEvaluationContext? schemaEvaluationContext)
    {
        if (!_responseEvaluationContext.TryEvaluate("content", out var responseContentEvaluationContext))
        {
            schemaEvaluationContext = null;
            return false;
        }

        if (!responseContentEvaluationContext.TryEvaluate("application/json", out var jsonContentEvaluationResults))
        {
            schemaEvaluationContext = null;
            return false;
        }

        schemaEvaluationContext = jsonContentEvaluationResults.Evaluate("schema");
        return true;
    }
    
    public void EvaluateHeaders(HttpResponseHeaders headers)
    {
        var headersEvaluationContext = _responseEvaluationContext.Evaluate("headers");
        foreach (var parameterEvaluationContext in headersEvaluationContext.EvaluateChildren())
        {
            var name = parameterEvaluationContext.GetKey();
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
            parameterSchemaEvaluationResults.EvaluateAgainstSchema(stringValues);
        }
    }
}