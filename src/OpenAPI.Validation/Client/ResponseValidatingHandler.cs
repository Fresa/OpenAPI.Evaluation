namespace OpenAPI.Validation.Client;

public class ResponseValidatingHandler : DelegatingHandler
{
    private readonly OpenApiDocument _openApiDocument;

    public ResponseValidatingHandler(OpenApiDocument openApiDocument, HttpMessageHandler inner) : base(inner)
    {
        _openApiDocument = openApiDocument;
    }

    /// <summary>
    /// Sends a request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="OpenApiEvaluationException"></exception>
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

        var responseEvaluation = await operationResponse.EvaluateAsync(response, cancellationToken)
            .ConfigureAwait(false);

        if (responseEvaluation.IsValid)
            return response;

        responseEvaluation.IgnoreValidResults();
        throw new OpenApiEvaluationException("The response message failed evaluation", responseEvaluation);
    }
}