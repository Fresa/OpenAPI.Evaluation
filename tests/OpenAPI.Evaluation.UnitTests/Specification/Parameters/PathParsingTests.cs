using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification.Parameters;

public class PathParsingTests
{
    [Theory]
    [InlineData("""
        {
            "in": "path",
            "name": "test",
            "required": true,
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
            "in": "path",
            "name": "test",
            "required": true,
            "schema": {
              "type": "string"
            }            
        }
        """, false)]
    [InlineData("""
        {
            "in": "path",
            "name": "test",
            "required": true
        }
        """, true)]
    [InlineData("""
        {
            "in": "path",
            "required": true
        }
        """, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test",
            "required": true
        }
        """, true)]
    [InlineData("""
        {
            "in": "path",
            "name": "test",
            "required": false,
            "schema": {
              "type": "string"
            }            
        }
        """, true)]
    [InlineData("""
        {
            "in": "path",
            "name": "test",
            "schema": {
              "type": "string"
            }            
        }
        """, true)]
    public void Given_a_path_parameter_When_parsing_It_should_be_parsed_according_to_spec(
        string json,
        bool shouldThrow)
    {
        var jsonNode = JsonNode.Parse(json);
        jsonNode.Should().NotBeNull();
        var reader = new JsonNodeReader(jsonNode!, JsonPointer.Empty);

        PathParameter? parameter;
        try
        {
            parameter = PathParameter.Parse(reader);
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }
        shouldThrow.Should().BeFalse();

        var @in = jsonNode.ShouldGetObject("/in").GetValue<string>();
        parameter!.In.Should().Be(@in);
        var name = jsonNode.ShouldGetObject("/name").GetValue<string>();
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