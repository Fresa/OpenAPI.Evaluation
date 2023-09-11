using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using OpenAPI.Evaluation.Specification;
using OpenAPI.Evaluation.UnitTests.Json;

namespace OpenAPI.Evaluation.UnitTests.Specification.Parameters;

public class HeaderTests
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
            }
        }
        """, false, true)]
    [InlineData("""
        {
            "in": "header",
            "name": "test",
            "schema": {
              "type": "string"
            }            
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
            "name": "Accept"
        }
        """, true, false)]
    [InlineData("""
        {
            "in": "header",
            "name": "Content-Type"
        }
        """, true, false)]
    [InlineData("""
        {
            "in": "header",
            "name": "Authorization"
        }
        """, true, false)]
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

        var @in = jsonNode.GetObject("/in").GetValue<string>();
        header!.In.Should().Be(@in);
        var name = jsonNode.GetObject("/name").GetValue<string>();
        header.Name.Should().Be(name);

        if (jsonNode.TryGetObject("/description", out var descriptionNode))
        {
            var description = descriptionNode.GetValue<string>();
            header.Description.Should().Be(description);
        }
        else
        {
            header.Description.Should().BeNull();
        }

        if (jsonNode.TryGetObject("/required", out var requiredNode))
        {
            var required = requiredNode.GetValue<bool>();
            header.Required.Should().Be(required);
        }
        else
        {
            header.Required.Should().BeFalse();
        }

        if (jsonNode.TryGetObject("/schema", out _))
        {
            header.Schema.Should().NotBeNull();
        }
        else
        {
            header.Schema.Should().BeNull();
        }

        if (jsonNode.TryGetObject("/content", out var contentNode))
        {
            var contentObject = contentNode.AsObject();
            header.Content.Should().NotBeNull();
            header.Content.Should().HaveCount(contentObject.Count);
        }
        else
        {
            header.Content.Should().BeNull();
        }
    }
}