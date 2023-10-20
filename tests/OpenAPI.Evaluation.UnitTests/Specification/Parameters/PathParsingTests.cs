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
            },
            "examples": [{
                "summary": "test1"
            }]
        }
        """, false)]
    [InlineData("""
        {
            "in": "path",
            "name": "test",
            "required": true,
            "schema": {
              "type": "string"
            },
            "style": "label",
            "description": "test",
            "deprecated": false,
            "explode": true,
            "example": "test"  
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
        parameter.ShouldBeEquivalentTo(jsonNode);
    }
}