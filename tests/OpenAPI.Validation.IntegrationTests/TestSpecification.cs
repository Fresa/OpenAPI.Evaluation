using OpenAPI.Validation.Client;
using OpenAPI.Validation.IntegrationTests.Http;
using Xunit.Abstractions;

namespace OpenAPI.Validation.IntegrationTests;

public abstract class TestSpecification : IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private CancellationTokenSource? _cts;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
    private readonly Uri _baseUri = new("http://localhost");

    protected TestSpecification(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Server = new HttpServer(_baseUri);
    }

    protected CancellationToken Timeout
    {
        get
        {
            _cts ??= new CancellationTokenSource(_timeout);
            return _cts.Token;
        }
    }

    internal HttpServer Server { get; }

    protected OpenApiDocument LoadOpenApiDocument(string pathRelativeToRoot)
        => OpenApi.OpenApi.Load(pathRelativeToRoot, _baseUri);

    protected HttpClient CreateResponseValidatingClient(OpenApiDocument document) =>
        new(new OpenApiEvaluationExceptionWriter(_testOutputHelper, 
            new ResponseValidatingHandler(document, Server)))
        {
            Timeout = _timeout,
            BaseAddress = _baseUri
        };

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _cts?.Dispose();
        return Task.CompletedTask;
    }
}