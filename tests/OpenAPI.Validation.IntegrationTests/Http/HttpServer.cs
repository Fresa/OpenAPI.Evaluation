using System.Net;
using System.Text;

namespace OpenAPI.Evaluation.IntegrationTests.Http;

internal sealed class HttpServer : HttpMessageHandler
{
    private readonly Uri _baseUri;
    private readonly Dictionary<(Uri, HttpMethod), Func<HttpResponseMessage>> _handlers = new();

    public HttpServer(Uri baseUri)
    {
        _baseUri = baseUri;
    }

    private static HttpContent CreateJsonContent(string json) => 
        new StringContent(json, Encoding.UTF8, "application/json");

    private HttpServer AddHandler(Uri uri, HttpMethod method, Func<HttpResponseMessage> createResponse)
    {
        _handlers.Add((
                uri.IsAbsoluteUri ? uri : new Uri(_baseUri, uri),
                method),
            createResponse);
        return this;
    }

    private HttpServer AddGetHandler(Uri uri, Func<HttpResponseMessage> createResponse) => 
        AddHandler(uri, HttpMethod.Get, createResponse);

    private HttpServer AddPostHandler(Uri uri, Func<HttpResponseMessage> createResponse) =>
        AddHandler(uri, HttpMethod.Post, createResponse);

    internal HttpServer AddGetWithNoResponse(Uri uri) => 
        AddGetHandler(uri, () => new HttpResponseMessage(HttpStatusCode.OK));
    internal HttpServer AddGetResponse(Uri uri, string json) =>
        AddGetHandler(uri, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateJsonContent(json)
        });
    internal HttpServer AddPostResponse(Uri uri, string json) =>
        AddPostHandler(uri, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateJsonContent(json)
        });

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => 
        HandleRequest(request);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(HandleRequest(request));

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        if (request.RequestUri == null)
            throw new ArgumentNullException($"{nameof(request)}.{nameof(request.RequestUri)}");

        if (!_handlers.TryGetValue((request.RequestUri, request.Method), out var createResponse))
            throw new InvalidOperationException(
                $"Missing handler for method {request.Method} and URI {request.RequestUri}");

        var response = createResponse();
        response.RequestMessage = request;
        return response;
    }
}