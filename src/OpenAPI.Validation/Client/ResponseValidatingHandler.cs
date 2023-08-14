using Json.Schema;
using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Client;

public class ResponseValidatingHandler : DelegatingHandler
{
    private readonly OpenApiDocument _openApiDocument;
    private readonly ValidatingOptions _options;

    public ResponseValidatingHandler(
        OpenApiDocument openApiDocument,
        ValidatingOptions options,
        HttpMessageHandler inner) : base(inner)
    {
        _openApiDocument = openApiDocument;
        _options = options;
    }

    /// <summary>
    /// Sends a request and validates the response according to the OpenAPI document
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="EvaluationHttpResponseMessage"/>The response message and evaluation results</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request doesn't match with any known api operation in the OpenAPI spec</exception>
    /// <exception cref="OpenApiEvaluationException">Thrown when the evaluation result is not valid and throwOnEvaluationFailure has been set</exception>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OpenApiOperation? operation = null;
        OpenApiEvaluationResults? evaluationResults = null;

        if (_options.ValidateRequest)
        {
            if (!_openApiDocument.TryGetApiOperation(
                    request, out operation, out evaluationResults))
            {
                // todo
                throw new OpenApiEvaluationException("Response failed evaluation", evaluationResults);
            }
            if (request.Content != null)
            {
                var requestContent = ReadContent(request.Content, cancellationToken);
                operation.EvaluateRequestContent(requestContent);
            }

            operation.EvaluateRequestHeaders(request.Headers);
            operation.EvaluateRequestPathParameters();
            operation.EvaluateRequestQueryParameters(request.RequestUri);
        }

        var response = base.Send(request, cancellationToken);

        if (!_options.ValidateResponse)
            return response;

        if (evaluationResults == null &&
            !_openApiDocument.TryGetApiOperation(
                request, out operation, out evaluationResults))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }
        var operationResponse = GetOpenApiOperationResponse(operation, request, response);

        var responseContent = ReadContent(response.Content, cancellationToken);
        operationResponse.EvaluateContent(responseContent);
        operationResponse.EvaluateHeaders(response.Headers);

        return CreateEvaluationResponseMessage(response, evaluationResults);
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
        OpenApiOperation? operation = null;
        OpenApiEvaluationResults? evaluationResults = null;

        if (_options.ValidateRequest)
        {
            if (!_openApiDocument.TryGetApiOperation(
                    request, out operation, out evaluationResults))
            {
                throw new InvalidOperationException("todo");
            }

            if (request.Content != null)
            {
                var requestContent = await ReadContentAsync(request.Content, cancellationToken)
                    .ConfigureAwait(false);
                operation.EvaluateRequestContent(requestContent);
            }

            operation.EvaluateRequestHeaders(request.Headers);
            operation.EvaluateRequestPathParameters();
            operation.EvaluateRequestQueryParameters(request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (!_options.ValidateResponse)
            return response;

        if (operation == null &&
            !_openApiDocument.TryGetApiOperation(
                    request, out operation, out evaluationResults))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }


        var operationResponse = GetOpenApiOperationResponse(operation, request, response);

        var responseContent = await ReadContentAsync(response.Content, cancellationToken)
            .ConfigureAwait(false);
        operationResponse.EvaluateContent(responseContent);
        operationResponse.EvaluateHeaders(response.Headers);

        return CreateEvaluationResponseMessage(response, evaluationResults!);
    }

    private OpenApiOperationResponse GetOpenApiOperationResponse(OpenApiOperation operation, HttpRequestMessage request, HttpResponseMessage response)
    {
        if (!operation.TryGetResponseSpecification(response.StatusCode, out var operationResponse))
        {
            throw new InvalidOperationException(
                $"There is no response with code {response.StatusCode} for operation {request.RequestUri}");
        }

        return operationResponse;
    }

    private static JsonNode? ReadContent(HttpContent httpContent, CancellationToken cancellationToken)
    {
        // Do not dispose the stream to let the user read it again (it get's disposed by the request/response message eventually)
        var contentStream = httpContent.ReadAsStream(cancellationToken);
        var content = JsonNode.Parse(contentStream);
        contentStream.Position = 0;
        return content;
    }

    private static async Task<JsonNode?> ReadContentAsync(HttpContent httpContent, CancellationToken cancellationToken)
    {
        // Do not dispose the stream to let the user read it again (it get's disposed by the request/response message eventually)
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var content = JsonNode.Parse(contentStream);
        contentStream.Position = 0;
        return content;
    }

    private EvaluationHttpResponseMessage CreateEvaluationResponseMessage(HttpResponseMessage response,
        OpenApiEvaluationResults evaluationResults)
    {
        var evaluationResponse = new EvaluationHttpResponseMessage(response, evaluationResults);
        if (_options.ThrowOnEvaluationFailure)
            evaluationResponse.ThrowIfOpenApiEvaluationIsNotValid();
        return evaluationResponse;
    }
}