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
    [InlineData("""
        {
            "url": "http://localhost/v1/user"
        }
        """, "http://localhost/v1/user", true)]
    [InlineData("""
        {
            "url": "http://localhost/v1/user"
        }
        """, "http://localhost/v1/user/", true)]
    [InlineData("""
        {
            "url": "http://localhost/v1/user/"
        }
        """, "http://localhost/v1/user", true)]
    [InlineData("""
        {
            "url": "http://localhost/v1/"
        }
        """, "http://localhost/v1/user", false)]
    [InlineData("""
        {
            "url": "http://localhost/v1/user"
        }
        """, "http://localhost/v1", false)]
    [InlineData("""
        {
            "url": "/v1/user/"
        }
        """, "http://localhost/v1/user", true)]
    [InlineData("""
        {
            "url": "/v1/user"
        }
        """, "http://localhost/v1/user/", true)]
    [InlineData("""
        {
            "url": "/v1/user"
        }
        """, "http://localhost/v1/user", true)]
    [InlineData("""
        {
            "url": "/v1/user"
        }
        """, "http://localhost/v1/user/1", false)]
    [InlineData("""
        {
            "url": "/v1/user/1"
        }
        """, "http://localhost/v1/user", false)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user/",
            "variables": {
                "host": {
                    "default": "localhost"
                }
            }
        }
        """, "http://localhost/v1/user", true)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user",
            "variables": {
                "host": {
                    "default": "foo"
                }
            }
        }
        """, "http://localhost/v1/user", false)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user",
            "variables": {
                "host": {
                    "default": "localhost",
                    "enum": [
                        "localhost",
                        "foo"
                    ]
                }
            }
        }
        """, "http://foo/v1/user", true)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user",
            "variables": {
                "host": {
                    "default": "localhost",
                    "enum": [
                        "localhost",
                        "foo"
                    ]
                }
            }
        }
        """, "http://bar/v1/user", false)]
    [InlineData("""
        {
            "url": "{scheme}://{host}/v1/{user}",
            "variables": {
                "host": {
                    "default": "localhost",
                    "enum": [
                        "localhost",
                        "foo"
                    ]
                },
                "scheme": {
                    "default": "https",
                    "enum": [
                        "http",
                        "https"
                    ]
                },
                "user": {
                    "default": "user"
                }
            }
        }
        """, "http://foo/v1/user/", true)]
    public void Given_server_urls_When_evaluating_urls_They_should_evaluate_correctly(string serverJson, string uri, bool valid)
    {
        var serverNode = JsonNode.Parse(serverJson);
        serverNode.Should().NotBeNull();
        var reader = new JsonNodeReader(serverNode!, JsonPointer.Empty);
        var server = Server.Parse(reader);
        var evaluationContext = new OpenApiEvaluationContext(
            reader, new OpenApiEvaluationOptions
            {
                Document = new JsonNodeBaseDocument(serverNode!, new Uri("http://localhost")),
                JsonSchemaEvaluationOptions = global::Json.Schema.EvaluationOptions.Default
            });
        var evaluator = server.GetEvaluator(evaluationContext);
        try
        {
            evaluator.TryMatch(new Uri(uri, UriKind.RelativeOrAbsolute)).Should().Be(valid);
        }
        finally
        {
            _testOutputHelper.WriteEvaluationResult(evaluationContext);
        }
        evaluationContext.Results.IsValid.Should().Be(valid);
    }
}