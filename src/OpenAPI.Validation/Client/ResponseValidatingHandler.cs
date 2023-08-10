using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Client;

public class ResponseValidatingHandler : DelegatingHandler
{
    private readonly OpenApiDocument _openApiDocument;
    private readonly bool _throwOnEvaluationFailure;

    public ResponseValidatingHandler(
        OpenApiDocument openApiDocument, 
        HttpMessageHandler inner, 
        bool throwOnEvaluationFailure = false) : base(inner)
    {
        _openApiDocument = openApiDocument;
        _throwOnEvaluationFailure = throwOnEvaluationFailure;
    }

    /// <summary>
    /// Sends a request and validates the response according to the OpenAPI document
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="EvaluationHttpResponseMessage"/>The response message and evaluation results</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request doesn't match with any known api operation in the OpenAPI spec</exception>
    /// <exception cref="OpenApiEvaluationException">Thrown when the evaluation result is not valid and throwOnEvaluationFailure has been set</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (!_openApiDocument.TryGetApiOperation(
                request, out var operation))
        {
            throw new InvalidOperationException(
                $"{request.Method} {request.RequestUri} should match an operation and path in the OpenAPI specification {_openApiDocument}");
        }

        if (!operation.TryGetResponseSpecification(response.StatusCode, out var operationResponse))
        {
            throw new InvalidOperationException(
                $"There is no response with code {response.StatusCode} for operation {request.RequestUri}");
        }

        // Do not dispose the stream to let the user read it again (it get's disposed by the response message eventually)
        var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var content = JsonNode.Parse(contentStream);

        operationResponse.EvaluateContent(content);
        operationResponse.EvaluateHeaders(response.Headers);
        var responseEvaluation = operationResponse.GetEvaluationResults();
        
        var evaluationResponse = new EvaluationHttpResponseMessage(response, responseEvaluation);
        if (_throwOnEvaluationFailure)
            evaluationResponse.ThrowIfOpenApiEvaluationIsNotValid();
        return evaluationResponse;
    }
}
