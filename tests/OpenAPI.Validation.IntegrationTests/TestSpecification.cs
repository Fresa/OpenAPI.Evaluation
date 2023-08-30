using OpenAPI.Evaluation.Client;
using OpenAPI.Evaluation.IntegrationTests.Http;
using Xunit.Abstractions;

namespace OpenAPI.Evaluation.IntegrationTests;

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

    protected Specification.OpenAPI LoadOpenApiDocument(string pathRelativeToRoot)
        => OpenApi.OpenApi.Load(pathRelativeToRoot, _baseUri);

    protected HttpClient CreateResponseValidatingClient(Specification.OpenAPI document) =>
        new(new OpenApiEvaluationResultWritingHandler(_testOutputHelper,
            new EvaluationHandler(document,
                new ValidatingOptions
                {
                    ThrowOnEvaluationFailure = true
                },
                Server)))
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