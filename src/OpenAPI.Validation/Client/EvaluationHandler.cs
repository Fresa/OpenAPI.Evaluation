using System.Net;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Http;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.Client;

public class EvaluationHandler : DelegatingHandler
{
    private readonly Specification.OpenAPI _openApi;
    private readonly ValidatingOptions _options;

    public EvaluationHandler(
        Specification.OpenAPI openApi,
        ValidatingOptions options,
        HttpMessageHandler inner) : base(inner)
    {
        _openApi = openApi;
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
        Operation.Evaluator? operationEvaluator = null;
        OpenApiEvaluationResults? evaluationResults = null;

        if (_options.ValidateRequest)
        {
            var requestUri = request.RequestUri ??
                             throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                                 "Request URI cannot be null");
            if (!_openApi.TryGetApiOperation(
                    request, out operationEvaluator, out evaluationResults))
            {
                return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                    evaluationResults);
            }
            if (request.Content != null)
            {
                var contentType = request.Content.Headers.ContentType ?? throw new ArgumentNullException(
                    $"{nameof(request)}.{nameof(request.Content)}.{nameof(request.Content.Headers)}.{request.Content.Headers.ContentType}",
                    "Missing request content header content-type");

                if (!operationEvaluator.TryMatchRequestContent(MediaTypeValue.Parse(contentType.ToString()),
                        out var requestMediaTypeEvaluator))
                {
                    return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                        evaluationResults);
                }

                var requestContent = ReadContent(request.Content, cancellationToken);
                requestMediaTypeEvaluator.EvaluateBody(requestContent);
            }
            else
            {
                operationEvaluator.EvaluateMissingRequestBody();
            }

            operationEvaluator.EvaluateRequestHeaders(request.Headers);
            operationEvaluator.EvaluateRequestPathParameters();
            operationEvaluator.EvaluateRequestQueryParameters(requestUri);
            operationEvaluator.EvaluateRequestCookies(requestUri, request.Headers);
        }

        var response = base.Send(request, cancellationToken);
        if (!_options.ValidateResponse)
            return response;

        if ((operationEvaluator == null || evaluationResults == null) &&
            !_openApi.TryGetApiOperation(
                request, out operationEvaluator, out evaluationResults))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        if (!operationEvaluator.TryMatchResponse((int)response.StatusCode, out var responseEvaluator))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        var responseContentType = response.Content.Headers.ContentType ?? throw new ArgumentNullException(
            $"{nameof(response)}.{nameof(response.Content)}.{nameof(response.Content.Headers)}.{response.Content.Headers.ContentType}",
            "Missing response content header content-type");

        if (!responseEvaluator.TryMatchResponseContent(MediaTypeValue.Parse(responseContentType.ToString()),
                out var responseMediaTypeEvaluator))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        var responseContent = ReadContent(response.Content, cancellationToken);
        responseMediaTypeEvaluator.EvaluateBody(responseContent);
        responseEvaluator.EvaluateHeaders(response.Headers);

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
        Operation.Evaluator? operationEvaluator = null;
        OpenApiEvaluationResults? evaluationResults = null;

        if (_options.ValidateRequest)
        {
            var requestUri = request.RequestUri ??
                             throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                                 "Request URI cannot be null");
            if (!_openApi.TryGetApiOperation(
                    request, out operationEvaluator, out evaluationResults))
            {
                return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                    evaluationResults);
            }
            if (request.Content != null)
            {
                var contentType = request.Content.Headers.ContentType ?? throw new ArgumentNullException(
                    $"{nameof(request)}.{nameof(request.Content)}.{nameof(request.Content.Headers)}.{request.Content.Headers.ContentType}",
                    "Missing content header content-type");

                if (!operationEvaluator.TryMatchRequestContent(MediaTypeValue.Parse(contentType.ToString()),
                        out var requestMediaTypeEvaluator))
                {
                    return CreateEvaluationResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest),
                        evaluationResults);
                }

                var requestContent = await ReadContentAsync(request.Content, cancellationToken)
                    .ConfigureAwait(false);
                requestMediaTypeEvaluator.EvaluateBody(requestContent);
            }
            else
            {
                operationEvaluator.EvaluateMissingRequestBody();
            }

            operationEvaluator.EvaluateRequestHeaders(request.Headers);
            operationEvaluator.EvaluateRequestPathParameters();
            operationEvaluator.EvaluateRequestQueryParameters(requestUri);
            operationEvaluator.EvaluateRequestCookies(requestUri, request.Headers);
        }

        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (!_options.ValidateResponse)
            return response;

        if ((operationEvaluator == null || evaluationResults == null) &&
            !_openApi.TryGetApiOperation(
                request, out operationEvaluator, out evaluationResults))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        if (!operationEvaluator.TryMatchResponse((int)response.StatusCode, out var responseEvaluator))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        var responseContentType = response.Content.Headers.ContentType ?? throw new ArgumentNullException(
            $"{nameof(response)}.{nameof(response.Content)}.{nameof(response.Content.Headers)}.{response.Content.Headers.ContentType}",
            "Missing response content header content-type");

        if (!responseEvaluator.TryMatchResponseContent(MediaTypeValue.Parse(responseContentType.ToString()),
                out var responseMediaTypeEvaluator))
        {
            return CreateEvaluationResponseMessage(response, evaluationResults);
        }

        var responseContent = await ReadContentAsync(response.Content, cancellationToken)
            .ConfigureAwait(false);
        responseMediaTypeEvaluator.EvaluateBody(responseContent);
        responseEvaluator.EvaluateHeaders(response.Headers);

        return CreateEvaluationResponseMessage(response, evaluationResults);
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