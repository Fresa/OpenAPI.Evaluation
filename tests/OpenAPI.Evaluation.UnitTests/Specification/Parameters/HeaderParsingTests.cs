using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification.Parameters;

public class HeaderParsingTests
{
    [Theory]
    [InlineData("""
        {
            "in": "header",
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
        """, false, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test",
            "schema": {
              "type": "string"
            },
            "style": "simple",
            "description": "test",
            "deprecated": false,
            "explode": true,
            "example": "test"  
        }
        """, false, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test"
        }
        """, true, false)]
    [InlineData("""
        {
            "in": "header"
        }
        """, true, false)]
    [InlineData("""
        {
            "in": "query",
            "name": "test"
        }
        """, true, false)]
    [InlineData("""
        {
            "in": "header",
            "name": "Accept",
            "schema": {
              "type": "string"
            }            
        
        }
        """, false, false)]
    [InlineData("""
        {
            "in": "header",
            "name": "Content-Type",
            "schema": {
              "type": "string"
            }            
        
        }
        """, false, false)]
    [InlineData("""
        {
            "in": "header",
            "name": "Authorization",
            "schema": {
              "type": "string"
            }            
        }
        """, false, false)]
    public void Given_a_header_parameter_When_parsing_It_should_be_parsed_according_to_spec(
        string json,
        bool shouldThrow,
        bool shouldParse)
    {
        var jsonNode = JsonNode.Parse(json);
        jsonNode.Should().NotBeNull();
        var reader = new JsonNodeReader(jsonNode!, JsonPointer.Empty);

        HeaderParameter? header;
        try
        {
            HeaderParameter.TryParseRequestHeader(reader, out header).Should().Be(shouldParse);
            if (!shouldParse)
                return;
        }
        catch
        {
            if (shouldThrow)
                return;
            throw;
        }
        shouldThrow.Should().BeFalse();
        header!.ShouldBeEquivalentTo(jsonNode);
    }
}