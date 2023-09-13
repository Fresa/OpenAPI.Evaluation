using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification.Parameters;

public class QueryParsingTests
{
    [Theory]
    [InlineData("""
        {
            "in": "query",
            "name": "test",
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
            "in": "query",
            "name": "test",
            "schema": {
              "type": "string"
            },
            "style": "form",
            "description": "test",
            "deprecated": false,
            "explode": true,
            "example": "test",
            "allowEmptyValue": true,
            "allowReserved": true
        }
        """, false)]
    [InlineData("""
        {
            "in": "query",
            "name": "test"
        }
        """, true)]
    [InlineData("""
        {
            "in": "query"
        }
        """, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test"
        }
        """, true)]
    public void Given_a_query_parameter_When_parsing_It_should_be_parsed_according_to_spec(
        string json,
        bool shouldThrow)
    {
        var jsonNode = JsonNode.Parse(json);
        jsonNode.Should().NotBeNull();
        var reader = new JsonNodeReader(jsonNode!, JsonPointer.Empty);

        QueryParameter? parameter;
        try
        {
            parameter = QueryParameter.Parse(reader);
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }
        shouldThrow.Should().BeFalse();
        parameter.ShouldBeEquivalentTo(jsonNode);
        
        _ = jsonNode.TryGetValue<bool>("/allowEmptyValue", out var allowEmptyValue)
            ? parameter.AllowEmptyValue.Should().Be(allowEmptyValue)
            : parameter.AllowEmptyValue.Should().BeFalse();

        _ = jsonNode.TryGetValue<bool>("/allowReserved", out var allowReserved)
            ? parameter.AllowReserved.Should().Be(allowReserved)
            : parameter.AllowReserved.Should().BeFalse();
    }
}