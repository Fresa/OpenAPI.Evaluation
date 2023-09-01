using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification;

public class ServerParsingTests
{
    [Theory]
    [InlineData("""
        {
            "url": "http://localhost/v1/user"          
        }
        """, false)]
    [InlineData("""
        {
            "url": "http://localhost/v1/user",
            "description": "test description",
            "variables": {
            }
        }
        """, false)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user",
            "variables": {
                "host": {
                    "default": "foo"
                }
            }
        }
        """, false)]
    [InlineData("""
        {
            "url": "http://{host}/v1/user",
            "variables": {                
            }
        }
        """, true)]
    [InlineData("""
        {
            "url": "http://{host/v1/user",
            "variables": {
                "host": {
                    "default": "foo"
                }
            }
        }
        """, true)]
    [InlineData("""
        {
            "url": "http://host}/v1/user",
            "variables": {
                "host": {
                    "default": "foo"
                }
            }
        }
        """, true)]
    public void Given_a_server_When_parsing_It_should_be_parsed_according_to_spec(
        string json,
        bool shouldThrow)
    {
        var jsonNode = JsonNode.Parse(json);
        jsonNode.Should().NotBeNull();
        var reader = new JsonNodeReader(jsonNode!, JsonPointer.Empty);

        Server server;
        try
        {
            server = Server.Parse(reader);
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }
        shouldThrow.Should().BeFalse();

        var url = jsonNode.GetObject("/url").GetValue<string>();
        server.Url.Should().Be(url);

        if (jsonNode.TryGetObject("/description", out var descriptionNode))
        {
            var description = descriptionNode.GetValue<string>();
            server.Description.Should().Be(description);
        }
        else
        {
            server.Description.Should().BeNull();
        }

        if (jsonNode.TryGetObject("/variables", out var variablesNode))
        {
            var variables = variablesNode.AsObject();
            server.Variables.Should().HaveCount(variables.Count);
        }
        else
        {
            server.Variables.Should().BeNull();
        }
    }
}