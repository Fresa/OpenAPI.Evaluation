using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Validation;

public sealed class OpenApiOperationResponse
{
    private readonly OpenApiEvaluationContext _responseEvaluationContext;
    private readonly EvaluationOptions _evaluationOptions;
    
    internal OpenApiOperationResponse(
        OpenApiEvaluationContext responseEvaluationContext,
        EvaluationOptions evaluationOptions)
    {
        _responseEvaluationContext = responseEvaluationContext;
        _evaluationOptions = evaluationOptions;
    }
    
    public void EvaluateContent(JsonNode? content)
    {
        if (!TryGetSchemaEvaluationContext(out var schemaEvaluationContext))
            return;

        schemaEvaluationContext.Evaluate(content, _evaluationOptions);
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
                    parameterEvaluationContext.Results.Report(
                        new JsonSchemaBuilder()
                            .Required(name)
                            .Evaluate(null, _evaluationOptions));
                }
                continue;
            }

            var parameterSchemaEvaluationResults = parameterEvaluationContext.Evaluate("schema");
            parameterSchemaEvaluationResults.Evaluate(stringValues, _evaluationOptions);
        }
    }

    public OpenApiEvaluationResults GetEvaluationResults() => _responseEvaluationContext.Results;
}

internal class JsonSchemaEvaluationException : JsonException
{
    public JsonSchemaEvaluationException(string message, List<EvaluationResults> evaluationResults) : base(message)
    {
        EvaluationResults = evaluationResults;
    }

    public JsonSchemaEvaluationException(string message, EvaluationResults evaluationResults) : base(message)
    {
        EvaluationResults = new List<EvaluationResults>
        {
            evaluationResults
        };
    }

    internal List<EvaluationResults> EvaluationResults { get; }
}