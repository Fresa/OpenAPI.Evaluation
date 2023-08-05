using System.Net;
using System.Text;

namespace OpenAPI.Validation.IntegrationTests.Http;

internal sealed class HttpServer : HttpMessageHandler
{
    private readonly Uri _baseUri;
    private readonly Dictionary<(Uri, HttpMethod), Func<HttpResponseMessage>> _handlers = new();

    public HttpServer(Uri baseUri)
    {
        _baseUri = baseUri;
    }

    internal HttpServer AddHandler(Uri uri, HttpMethod method, Func<HttpResponseMessage> createResponse)
    {
        _handlers.Add((
                uri.IsAbsoluteUri ? uri : new Uri(_baseUri, uri),
                method),
            createResponse);
        return this;
    }

    internal HttpServer AddGetHandler(Uri uri, Func<HttpResponseMessage> createResponse) => 
        AddHandler(uri, HttpMethod.Get, createResponse);

    internal HttpServer AddGetResponse(Uri uri, string json) =>
        AddGetHandler(uri, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
            throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}");

        if (!_handlers.TryGetValue((request.RequestUri, request.Method), out var createResponse))
            throw new InvalidOperationException(
                $"Missing handler for method {request.Method} and URI {request.RequestUri}");

        var response = createResponse();
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}