using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.XUnit;
using Xunit.Abstractions;

namespace OpenAPI.Evaluation.UnitTests.Specification;

public class ServerEvaluationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ServerEvaluationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("http://localhost/v1/user", "http://localhost/v1/user", true)]
    [InlineData("http://localhost/v1/user", "http://localhost/v1/user/", true)]
    [InlineData("http://localhost/v1/user/", "http://localhost/v1/user", true)]
    [InlineData("http://localhost/v1/", "http://localhost/v1/user", false)]
    [InlineData("http://localhost/v1/user", "http://localhost/v1", false)]
    [InlineData("/v1/user", "http://localhost/v1/user", false)]
    [InlineData("/v1/user", "/v1/user", true)]
    [InlineData("http://localhost/v1/user", "/v1/user", false)]
    [InlineData("/v1/user", "/v1/user/1", false)]
    [InlineData("/v1/user/1", "/v1/user", false)]
    public void Given_server_urls_When_evaluating_urls_They_should_evaluate_correctly(string serverUrl, string uri, bool valid)
    {
        var serverJson = JsonNode.Parse($$"""
            {
                "url": "{{serverUrl}}"
            }
            """);
        serverJson.Should().NotBeNull();
        var reader = new JsonNodeReader(serverJson!, JsonPointer.Empty);
        var server = Server.Parse(reader);
        var evaluationContext = new OpenApiEvaluationContext(
            new JsonNodeBaseDocument(serverJson!, new Uri("http://localhost")), reader, EvaluationOptions.Default);
        var evaluator = server.GetEvaluator(evaluationContext);
        evaluator.TryMatch(new Uri(uri, UriKind.RelativeOrAbsolute)).Should().Be(valid);
        _testOutputHelper.WriteEvaluationResult(evaluationContext);
        evaluationContext.Results.IsValid.Should().Be(valid);
    }
}