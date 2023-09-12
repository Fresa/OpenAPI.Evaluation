using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification.Parameters;

public class CookieParsingTests
{
    [Theory]
    [InlineData("""
        {
            "in": "cookie",
            "name": "test",
            "content": {
              "application/json": {
                "schema": {
                  "type": "string"
                }
              }
            }
        }
        """, false)]
    [InlineData("""
        {
            "in": "cookie",
            "name": "test",
            "schema": {
              "type": "string"
            }            
        }
        """, false)]
    [InlineData("""
        {
            "in": "cookie",
            "name": "test"
        }
        """, true)]
    [InlineData("""
        {
            "in": "cookie"
        }
        """, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test"
        }
        """, true)]
    public void Given_a_cookie_parameter_When_parsing_It_should_be_parsed_according_to_spec(
        string json,
        bool shouldThrow)
    {
        var jsonNode = JsonNode.Parse(json);
        jsonNode.Should().NotBeNull();
        var reader = new JsonNodeReader(jsonNode!, JsonPointer.Empty);

        CookieParameter? parameter;
        try
        {
            parameter = CookieParameter.Parse(reader);
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }
        shouldThrow.Should().BeFalse();

        var @in = jsonNode.GetObject("/in").GetValue<string>();
        parameter!.In.Should().Be(@in);
        var name = jsonNode.GetObject("/name").GetValue<string>();
        parameter.Name.Should().Be(name);

        if (jsonNode.TryGetObject("/description", out var descriptionNode))
        {
            var description = descriptionNode.GetValue<string>();
            parameter.Description.Should().Be(description);
        }
        else
        {
            parameter.Description.Should().BeNull();
        }

        if (jsonNode.TryGetObject("/required", out var requiredNode))
        {
            var required = requiredNode.GetValue<bool>();
            parameter.Required.Should().Be(required);
        }
        else
        {
            parameter.Required.Should().BeFalse();
        }

        if (jsonNode.TryGetObject("/schema", out _))
        {
            parameter.Schema.Should().NotBeNull();
        }
        else
        {
            parameter.Schema.Should().BeNull();
        }

        if (jsonNode.TryGetObject("/content", out var contentNode))
        {
            var contentObject = contentNode.AsObject();
            parameter.Content.Should().NotBeNull();
            parameter.Content.Should().HaveCount(contentObject.Count);
        }
        else
        {
            parameter.Content.Should().BeNull();
        }
    }
}