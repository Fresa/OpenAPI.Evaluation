using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;

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
            },
            "examples": [{
                "summary": "test1"
            }]
        }
        """, false)]
    [InlineData("""
        {
            "in": "cookie",
            "name": "test",
            "schema": {
              "type": "string"
            },
            "style": "form",
            "deprecated": false,
            "explode": true,
            "example": "test"
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
        parameter.ShouldBeEquivalentTo(jsonNode);
    }
}