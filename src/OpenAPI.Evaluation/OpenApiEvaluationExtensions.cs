using System.Collections.Immutable;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation;

public static class OpenApiEvaluationExtensions
{
    public static OpenApiEvaluationResults EvaluateRequest(this Specification.OpenAPI openApiSpecification,
        Uri requestUri, 
        string method, 
        IDictionary<string, IEnumerable<string>>? requestHeaders = null, 
        JsonNode? requestContent = null)
    {
        if (!openApiSpecification.TryMatchApiOperation(
                requestUri, method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        requestHeaders ??= ImmutableDictionary<string, IEnumerable<string>>.Empty;
        if (requestContent != null)
        {
            var contentType =
                requestHeaders.FirstOrDefault(pair => pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    .Value?.FirstOrDefault() ?? throw new ArgumentException("Missing header 'Content-Type'");
            if (operationEvaluator.TryMatchRequestContent(contentType,
                    out var requestMediaTypeEvaluator))
            {
                requestMediaTypeEvaluator.Evaluate(requestContent);
            }
        }
        else
        {
            operationEvaluator.EvaluateMissingRequestBody();
        }

        operationEvaluator.EvaluateRequestHeaders(requestHeaders);
        operationEvaluator.EvaluateRequestPathParameters();
        operationEvaluator.EvaluateRequestQueryParameters(requestUri);
        operationEvaluator.EvaluateRequestCookies(requestUri, requestHeaders);
        return evaluationResults;
    }

    public static OpenApiEvaluationResults EvaluateResponse(this Specification.OpenAPI openApiSpecification,
        Uri requestUri,
        string method,
        int responseStatusCode,
        IDictionary<string, IEnumerable<string>>? responseHeaders = null,
        JsonNode? responseContent = null)
    {
        if (!openApiSpecification.TryMatchApiOperation(
                requestUri, method, out var operationEvaluator, out var evaluationResults))
        {
            return evaluationResults;
        }

        if (!operationEvaluator.TryMatchResponse(responseStatusCode, out var responseEvaluator))
        {
            return evaluationResults;
        }

        responseHeaders ??= ImmutableDictionary<string, IEnumerable<string>>.Empty;
        responseEvaluator.EvaluateHeaders(responseHeaders);

        var responseContentType =
            responseHeaders.FirstOrDefault(pair => pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                .Value?.FirstOrDefault();
        var mediaType = responseContentType == null ? null : MediaTypeValue.Parse(responseContentType);
        if (!responseEvaluator.TryMatchResponseContent(
                mediaType,
                out var responseMediaTypeEvaluator))
        {
            return evaluationResults;
        }

        responseMediaTypeEvaluator.Evaluate(responseContent);

        return evaluationResults;
    }

    public static OpenApiEvaluationResults EvaluateRequest(this Specification.OpenAPI openApiSpecification,
        HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var requestUri = request.GetRequestUri();
        var method = request.Method.Method;
        var body = request.Content.Read(cancellationToken);
        var headers = request.Headers.ToImmutableDictionary();
        if (request.Content != null)
        {
            headers = headers.AddRange(request.Content.Headers);
        }
        return openApiSpecification.EvaluateRequest(requestUri, method, headers, body);
    }

    public static async Task<OpenApiEvaluationResults> EvaluateRequestAsync(this Specification.OpenAPI openApiSpecification,
        HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var requestUri = request.GetRequestUri();
        var method = request.Method.Method;
        var body = await request.Content.ReadAsync(cancellationToken)
            .ConfigureAwait(false);
        var headers = request.Headers.ToImmutableDictionary();
        if (request.Content != null)
        {
            headers = headers.AddRange(request.Content.Headers);
        }
        return openApiSpecification.EvaluateRequest(requestUri, method, headers, body);
    }

    public static OpenApiEvaluationResults EvaluateResponse(this Specification.OpenAPI openApiSpecification,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var request = response.GetRequestMessage();
        var requestUri = request.GetRequestUri();
        var method = request.Method.Method;
        var responseHeaders = response
            .Headers.ToImmutableDictionary()
            .AddRange(response.Content.Headers);
        var responseCode = (int)response.StatusCode;
        var responseContent = response.Content.Read(cancellationToken);

        return openApiSpecification.EvaluateResponse(requestUri, method, responseCode, responseHeaders,
            responseContent);
    }

    public static async Task<OpenApiEvaluationResults> EvaluateResponseAsync(this Specification.OpenAPI openApiSpecification,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var request = response.GetRequestMessage();
        var requestUri = request.GetRequestUri();
        var method = request.Method.Method;
        var responseHeaders = response
            .Headers.ToImmutableDictionary()
            .AddRange(response.Content.Headers);
        var responseCode = (int)response.StatusCode;
        var responseContent = await response.Content.ReadAsync(cancellationToken)
            .ConfigureAwait(false);

        return openApiSpecification.EvaluateResponse(requestUri, method, responseCode, responseHeaders,
            responseContent);
    }

    private static Uri GetRequestUri(this HttpRequestMessage request) =>
        request.RequestUri ??
        throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}",
            "Request URI cannot be null");
    private static HttpRequestMessage GetRequestMessage(this HttpResponseMessage response) =>
        response.RequestMessage ??
        throw new ArgumentNullException(
            $"{nameof(response)}.{nameof(response.RequestMessage)}", "Request message is null");

    private static JsonNode? Read(this HttpContent? httpContent, CancellationToken cancellationToken)
    {
        if (httpContent == null)
            return null;
        // Do not dispose the stream to let the user read it again (it get's disposed by the request/response message eventually)
        var contentStream = httpContent.ReadAsStream(cancellationToken);
        if (contentStream.ReadByte() == -1)
        {
            return null;
        }
        contentStream.Position = 0;
        var content = JsonNode.Parse(contentStream);
        contentStream.Position = 0;
        return content;
    }

    private static async Task<JsonNode?> ReadAsync(this HttpContent? httpContent, CancellationToken cancellationToken)
    {
        if (httpContent == null)
            return null;

        await httpContent.LoadIntoBufferAsync()
            .ConfigureAwait(false);
        // Do not dispose the stream to let the user read it again (it get's disposed by the request/response message eventually)
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var buffer = new byte[1];
        var result = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (result == 0)
        {
            return null;
        }
        contentStream.Position = 0;
        var content = JsonNode.Parse(contentStream);
        contentStream.Position = 0;
        return content;
    }
}