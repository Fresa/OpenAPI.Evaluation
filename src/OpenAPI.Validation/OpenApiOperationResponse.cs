using System.Net.Http.Headers;
using System.Text.Json;
using Json.Schema;

namespace OpenAPI.Validation;

public sealed class OpenApiOperationResponse
{
    private readonly JsonNodeReader _responseNodeReader;
    private readonly JsonNodeBaseDocument _baseDocument;
    private readonly EvaluationOptions _evaluationOptions;

    internal OpenApiOperationResponse(
        JsonNodeReader responseNodeReader,
        JsonNodeBaseDocument baseDocument,
        EvaluationOptions evaluationOptions)
    {
        _responseNodeReader = responseNodeReader;
        _baseDocument = baseDocument;
        _evaluationOptions = evaluationOptions;
    }

    public async Task<OpenApiEvaluationResults> EvaluateAsync(HttpResponseMessage message, CancellationToken cancellationToken = default)
    {
        var responseEvaluationContext = new OpenApiEvaluationContext(_baseDocument, _responseNodeReader);
        await EvaluateJsonResponseContentAsync(message.Content, responseEvaluationContext, cancellationToken)
            .ConfigureAwait(false);
        EvaluateResponseHeaders(message.Headers, responseEvaluationContext);
        return responseEvaluationContext.Results;
    }

    private async Task EvaluateJsonResponseContentAsync(HttpContent content,
        OpenApiEvaluationContext evaluationContext, CancellationToken cancellationToken)
    {
        if (!evaluationContext.TryEvaluate("content", out var responseContentEvaluationContext))
            return;

        if (!responseContentEvaluationContext.TryEvaluate("application/json", out var jsonContentEvaluationResults))
            return;

        var schemaEvaluationContext = jsonContentEvaluationResults.Evaluate("schema");
        
        await content.LoadIntoBufferAsync()
            .ConfigureAwait(false);
        // Do not close/dispose the stream to let the caller use it later for deserialization.
        // The stream will be cached by HttpContent and disposed by the owner of the HttpResponseMessage
        var stream = await content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        stream.Position = 0;
        schemaEvaluationContext.Validate(document, _evaluationOptions);
    }

    private void EvaluateResponseHeaders(HttpResponseHeaders headers, OpenApiEvaluationContext responseEvaluationContext)
    {
        var headersEvaluationContext = responseEvaluationContext.Evaluate("headers");
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
            parameterSchemaEvaluationResults.Validate(stringValues, _evaluationOptions);
        }
    }
}