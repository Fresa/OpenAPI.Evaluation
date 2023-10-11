using System.Collections.Immutable;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation;

public static class OpenApiEvaluationExtensions
{
    public static OpenApiEvaluationResults Evaluate(this Specification.OpenAPI openApiSpecification,
        HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var requestUri = request.RequestUri ??
                         throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                             "Request URI cannot be null");
        if (!openApiSpecification.TryMatchApiOperation(
                request.RequestUri, request.Method.Method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        if (request.Content != null)
        {
            var contentType = request.Content.Headers.ContentType ?? throw new ArgumentNullException(
                $"{nameof(request)}.{nameof(request.Content)}.{nameof(request.Content.Headers)}.{request.Content.Headers.ContentType}",
                "Missing content header content-type");

            if (operationEvaluator.TryMatchRequestContent(contentType.ToString(),
                    out var requestMediaTypeEvaluator))
            {
                var requestContent = ReadContent(request.Content, cancellationToken);
                requestMediaTypeEvaluator.Evaluate(requestContent);
            }
        }
        else
        {
            operationEvaluator.EvaluateMissingRequestBody();
        }

        var headers = request.Headers.ToImmutableDictionary();
        if (request.Content != null)
        {
            headers = headers.AddRange(request.Content.Headers);
        }
        operationEvaluator.EvaluateRequestHeaders(headers);
        operationEvaluator.EvaluateRequestPathParameters();
        operationEvaluator.EvaluateRequestQueryParameters(requestUri);
        operationEvaluator.EvaluateRequestCookies(requestUri, headers);
        return evaluationResults;
    }

    public static async Task<OpenApiEvaluationResults> EvaluateAsync(this Specification.OpenAPI openApiSpecification,
        HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var requestUri = request.RequestUri ??
                         throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                             "Request URI cannot be null");
        if (!openApiSpecification.TryMatchApiOperation(
                request.RequestUri, request.Method.Method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        if (request.Content != null)
        {
            var contentType = request.Content.Headers.ContentType ?? throw new ArgumentNullException(
                $"{nameof(request)}.{nameof(request.Content)}.{nameof(request.Content.Headers)}.{request.Content.Headers.ContentType}",
                "Missing content header content-type");

            if (operationEvaluator.TryMatchRequestContent(contentType.ToString(),
                    out var requestMediaTypeEvaluator))
            {
                var requestContent = await ReadContentAsync(request.Content, cancellationToken)
                    .ConfigureAwait(false);
                requestMediaTypeEvaluator.Evaluate(requestContent);
            }
        }
        else
        {
            operationEvaluator.EvaluateMissingRequestBody();
        }

        var headers = request.Headers.ToImmutableDictionary();
        if (request.Content != null)
        {
            headers = headers.AddRange(request.Content.Headers);
        }
        operationEvaluator.EvaluateRequestHeaders(headers);
        operationEvaluator.EvaluateRequestPathParameters();
        operationEvaluator.EvaluateRequestQueryParameters(requestUri);
        operationEvaluator.EvaluateRequestCookies(requestUri, headers);
        return evaluationResults;
    }

    public static OpenApiEvaluationResults Evaluate(this Specification.OpenAPI openApiSpecification,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var request = response.RequestMessage ??
                      throw new ArgumentNullException(
                          $"{nameof(response)}.{nameof(response.RequestMessage)}", "Request message is null");

        if (request.RequestUri == null)
            throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                "Request URI cannot be null");


        if (!openApiSpecification.TryMatchApiOperation(
                request.RequestUri, request.Method.Method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        if (!operationEvaluator.TryMatchResponse((int)response.StatusCode, out var responseEvaluator))
        {
            return evaluationResults;
        }

        responseEvaluator.EvaluateHeaders(response.Headers.ToImmutableDictionary());

        var responseContentType = response.Content.Headers.ContentType;
        var mediaType = responseContentType == null ? null : MediaTypeValue.Parse(responseContentType.ToString());
        if (!responseEvaluator.TryMatchResponseContent(
                mediaType,
                out var responseMediaTypeEvaluator))
        {
            return evaluationResults;
        }

        var responseContent = ReadContent(response.Content, cancellationToken);
        responseMediaTypeEvaluator.Evaluate(responseContent);
        
        return evaluationResults;
    }

    public static async Task<OpenApiEvaluationResults> EvaluateAsync(this Specification.OpenAPI openApiSpecification,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var request = response.RequestMessage ??
                      throw new ArgumentNullException(
                          $"{nameof(response)}.{nameof(response.RequestMessage)}", "Request message is null");

        if (request.RequestUri == null)
            throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
                "Request URI cannot be null");


        if (!openApiSpecification.TryMatchApiOperation(
                request.RequestUri, request.Method.Method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        if (!operationEvaluator.TryMatchResponse((int)response.StatusCode, out var responseEvaluator))
        {
            return evaluationResults;
        }

        responseEvaluator.EvaluateHeaders(response.Headers.ToImmutableDictionary());

        var responseContentType = response.Content.Headers.ContentType;
        var mediaType = responseContentType == null ? null : MediaTypeValue.Parse(responseContentType.ToString());
        if (!responseEvaluator.TryMatchResponseContent(
                mediaType,
                out var responseMediaTypeEvaluator))
        {
            return evaluationResults;
        }

        var responseContent = await ReadContentAsync(response.Content, cancellationToken)
            .ConfigureAwait(false);
        responseMediaTypeEvaluator.Evaluate(responseContent);

        return evaluationResults;
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
}